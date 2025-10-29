using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using quickLink.Helpers;

namespace quickLink.ViewModels
{
    public class AddEditDialogViewModel : INotifyPropertyChanged
  {
 private string _title = string.Empty;
        private string _value = string.Empty;
      private bool _isEncrypted;

        public string Title
     {
get => _title;
       set => SetProperty(ref _title, value);
     }

   public string Value
  {
   get => _value;
set => SetProperty(ref _value, value);
  }

   public bool IsEncrypted
  {
            get => _isEncrypted;
    set => SetProperty(ref _isEncrypted, value);
 }

   public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public bool IsSaved { get; private set; }

public AddEditDialogViewModel()
        {
  SaveCommand = new RelayCommand(Save, CanSave);
       CancelCommand = new RelayCommand(Cancel);
  }

        public void SetEditMode(string title, string value, bool isEncrypted)
   {
   Title = title;
      Value = value;
            IsEncrypted = isEncrypted;
    }

        private bool CanSave()
     {
  return !string.IsNullOrWhiteSpace(Value);
        }

  private void Save()
        {
     IsSaved = true;
        }

        private void Cancel()
   {
    IsSaved = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

   protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
  {
        if (Equals(field, value)) return false;
  field = value;
   OnPropertyChanged(propertyName);
      
            // Raise CanExecute for Save command when Value changes
   if (propertyName == nameof(Value) && SaveCommand is RelayCommand cmd)
{
        cmd.RaiseCanExecuteChanged();
 }
       
     return true;
  }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
{
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
 }
    }
}
