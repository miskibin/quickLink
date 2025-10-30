using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using quickLink.Helpers;
using quickLink.Models;
using quickLink.Services;

namespace quickLink.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
  private readonly DataService _dataService;
   private readonly ClipboardService _clipboardService;
        private string _searchText = string.Empty;
   private ObservableCollection<ClipboardItem> _allItems;
     private ObservableCollection<ClipboardItem> _filteredItems;
        private ClipboardItem? _selectedItem;
    private bool _isLoading;

        public MainViewModel()
  {
  _dataService = new DataService();
    _clipboardService = new ClipboardService();
     _allItems = new ObservableCollection<ClipboardItem>();
   _filteredItems = new ObservableCollection<ClipboardItem>();

  AddCommand = new RelayCommand(async () => await AddItemAsync());
       ExecuteCommand = new RelayCommand(async () => await ExecuteSelectedItemAsync(), () => SelectedItem != null);
        EditCommand = new RelayCommand<ClipboardItem>(async (item) => await EditItemAsync(item));
  DeleteCommand = new RelayCommand<ClipboardItem>(async (item) => await DeleteItemAsync(item));
      }

        public string SearchText
 {
get => _searchText;
  set
  {
      if (SetProperty(ref _searchText, value))
   {
        FilterItems();
          }
 }
        }

    public ObservableCollection<ClipboardItem> FilteredItems
   {
      get => _filteredItems;
     set => SetProperty(ref _filteredItems, value);
   }

   public ClipboardItem? SelectedItem
 {
   get => _selectedItem;
      set
    {
    if (SetProperty(ref _selectedItem, value))
    {
         (ExecuteCommand as RelayCommand)?.RaiseCanExecuteChanged();
       }
  }
  }

    public bool IsLoading
  {
        get => _isLoading;
  set => SetProperty(ref _isLoading, value);
        }

public ICommand AddCommand { get; }
        public ICommand ExecuteCommand { get; }
        public ICommand EditCommand { get; }
      public ICommand DeleteCommand { get; }

        public async Task InitializeAsync()
        {
 IsLoading = true;
  try
 {
       var items = await _dataService.LoadItemsAsync();
_allItems.Clear();
    foreach (var item in items)
           {
          _allItems.Add(item);
     }
     FilterItems();
        }
  finally
   {
      IsLoading = false;
            }
 }

  private void FilterItems()
        {
   FilteredItems.Clear();

       var query = _allItems.AsEnumerable();

      if (!string.IsNullOrWhiteSpace(SearchText))
  {
     var search = SearchText.ToLowerInvariant();
       query = query.Where(item =>
     (!string.IsNullOrWhiteSpace(item.Title) && item.Title.ToLowerInvariant().Contains(search)) ||
    (!string.IsNullOrWhiteSpace(item.Value) && item.Value.ToLowerInvariant().Contains(search)));
    }

       foreach (var item in query)
       {
    FilteredItems.Add(item);
  }

  // Auto-select first item
  SelectedItem = FilteredItems.FirstOrDefault();
        }

        public async Task ExecuteSelectedItemAsync()
        {
    if (SelectedItem == null) return;

    var value = SelectedItem.Value;

        if (SelectedItem.IsLink)
       {
  await _clipboardService.OpenUrlAsync(value);
       }
   else
       {
    _clipboardService.CopyToClipboard(value);
      }
 }

     private Task AddItemAsync()
   {
   var dialog = new Views.AddEditDialog();
       // Dialog will be shown by the caller (MainWindow)
       OnAddEditDialogRequested(dialog);
       return Task.CompletedTask;
      }

private Task EditItemAsync(ClipboardItem? item)
 {
       if (item == null) return Task.CompletedTask;

  var dialog = new Views.AddEditDialog();
   dialog.ViewModel.SetEditMode(item.Title, item.Value, item.IsEncrypted);
   dialog.Tag = item; // Store original item for reference
OnAddEditDialogRequested(dialog);
       return Task.CompletedTask;
 }

   private async Task DeleteItemAsync(ClipboardItem? item)
  {
    if (item == null) return;

  _allItems.Remove(item);
 await _dataService.DeleteItemAsync(item, _allItems.ToList());
  FilterItems();
    }

        public async Task SaveNewItemAsync(Views.AddEditDialog dialog)
        {
     if (!dialog.ViewModel.IsSaved) return;

       var newItem = new ClipboardItem
   {
     Title = dialog.ViewModel.Title,
Value = dialog.ViewModel.Value,
     IsEncrypted = dialog.ViewModel.IsEncrypted
       };

  await _dataService.AddItemAsync(newItem, _allItems.ToList());
    _allItems.Add(newItem);
   FilterItems();
 }

   public async Task SaveEditedItemAsync(Views.AddEditDialog dialog)
{
 if (!dialog.ViewModel.IsSaved || dialog.Tag is not ClipboardItem originalItem) return;

    var updatedItem = new ClipboardItem
{
    Title = dialog.ViewModel.Title,
 Value = dialog.ViewModel.Value,
   IsEncrypted = dialog.ViewModel.IsEncrypted
    };

await _dataService.UpdateItemAsync(originalItem, updatedItem, _allItems.ToList());
     
       var index = _allItems.IndexOf(originalItem);
     if (index >= 0)
     {
    _allItems[index] = updatedItem;
  }
       
        FilterItems();
}

    // Event for MainWindow to handle dialog display
        public event EventHandler<Views.AddEditDialog>? AddEditDialogRequested;

        private void OnAddEditDialogRequested(Views.AddEditDialog dialog)
        {
    AddEditDialogRequested?.Invoke(this, dialog);
 }

    public event PropertyChangedEventHandler? PropertyChanged;

  protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
      if (Equals(field, value)) return false;
field = value;
            OnPropertyChanged(propertyName);
     return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
     {
   PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
