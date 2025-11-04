using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using quickLink.Helpers;
using quickLink.Models;

namespace quickLink.ViewModels
{
    public class AddEditCommandViewModel : INotifyPropertyChanged
    {
        private string _prefix = "/";
        private CommandSourceType _selectedSource = CommandSourceType.Directory;
        private string _path = string.Empty;
        private bool _recursive = true;
        private string _glob = "*.md";
        private string _executeTemplate = string.Empty;
        private CommandIcon _selectedIcon = CommandIcon.Folder;
        private bool _isSaved;

        public AddEditCommandViewModel()
        {
            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            
            // Set default path to Documents folder
            _path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _executeTemplate = "code \"{item.path}\"";
        }

        public string Prefix
        {
            get => _prefix;
            set
            {
                if (SetProperty(ref _prefix, value))
                {
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public CommandSourceType SelectedSource
        {
            get => _selectedSource;
            set => SetProperty(ref _selectedSource, value);
        }

        public string Path
        {
            get => _path;
            set
            {
                if (SetProperty(ref _path, value))
                {
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool Recursive
        {
            get => _recursive;
            set => SetProperty(ref _recursive, value);
        }

        public string Glob
        {
            get => _glob;
            set
            {
                if (SetProperty(ref _glob, value))
                {
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string ExecuteTemplate
        {
            get => _executeTemplate;
            set
            {
                if (SetProperty(ref _executeTemplate, value))
                {
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public CommandIcon SelectedIcon
        {
            get => _selectedIcon;
            set => SetProperty(ref _selectedIcon, value);
        }

        public bool IsSaved
        {
            get => _isSaved;
            private set => SetProperty(ref _isSaved, value);
        }

        public List<CommandSourceType> SourceTypes { get; } = new()
        {
            CommandSourceType.Directory,
            CommandSourceType.Static
        };

        public List<IconOption> IconOptions { get; } = new()
        {
            new IconOption { Icon = CommandIcon.Folder, Display = "📁 Folder", Name = "Folder" },
            new IconOption { Icon = CommandIcon.Web, Display = "🌐 Web", Name = "Web" },
            new IconOption { Icon = CommandIcon.Script, Display = "⚙️ Script", Name = "Script" },
            new IconOption { Icon = CommandIcon.Document, Display = "📄 Document", Name = "Document" }
        };

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public void SetEditMode(UserCommand command)
        {
            Prefix = command.Prefix;
            SelectedSource = command.Source;
            Path = command.SourceConfig.Path;
            Recursive = command.SourceConfig.Recursive;
            Glob = command.SourceConfig.Glob;
            ExecuteTemplate = command.ExecuteTemplate;
            SelectedIcon = command.Icon;
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Prefix) &&
                   !string.IsNullOrWhiteSpace(ExecuteTemplate) &&
                   (SelectedSource != CommandSourceType.Directory || !string.IsNullOrWhiteSpace(Path)) &&
                   (SelectedSource != CommandSourceType.Directory || !string.IsNullOrWhiteSpace(Glob));
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
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class IconOption
    {
        public CommandIcon Icon { get; set; }
        public string Display { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
