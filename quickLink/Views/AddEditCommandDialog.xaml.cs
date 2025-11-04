using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using quickLink.Models;
using quickLink.ViewModels;

namespace quickLink.Views
{
    public sealed partial class AddEditCommandDialog : ContentDialog
    {
        public AddEditCommandViewModel ViewModel { get; }

        public AddEditCommandDialog()
        {
            ViewModel = new AddEditCommandViewModel();
            this.DataContext = ViewModel;
            InitializeComponent();
        }

        private Visibility IsDirectorySource(CommandSourceType source)
        {
            return source == CommandSourceType.Directory ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
