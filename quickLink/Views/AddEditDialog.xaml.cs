using Microsoft.UI.Xaml.Controls;
using quickLink.ViewModels;

namespace quickLink.Views
{
    public sealed partial class AddEditDialog : ContentDialog
    {
        public AddEditDialogViewModel ViewModel { get; }

        public AddEditDialog()
        {
            ViewModel = new AddEditDialogViewModel();
            this.DataContext = ViewModel;
            InitializeComponent();
        }
    }
}
