using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace quickLink.Models
{
    public enum CommandSourceType
    {
        Directory,
        Static,
        Http
    }

    public enum CommandIcon
    {
        Folder,    // 📁
        Web,       // 🌐
        Script,    // ⚙️
        Document   // 📄
    }

    public class UserCommand : INotifyPropertyChanged
    {
        private string _prefix = string.Empty;
        private CommandSourceType _source = CommandSourceType.Directory;
        private SourceConfig _sourceConfig = new SourceConfig();
        private string _executeTemplate = string.Empty;
        private CommandIcon _icon = CommandIcon.Folder;

        public string Prefix
        {
            get => _prefix;
            set => SetProperty(ref _prefix, value);
        }

        public CommandSourceType Source
        {
            get => _source;
            set => SetProperty(ref _source, value);
        }

        public SourceConfig SourceConfig
        {
            get => _sourceConfig;
            set => SetProperty(ref _sourceConfig, value);
        }

        public string ExecuteTemplate
        {
            get => _executeTemplate;
            set => SetProperty(ref _executeTemplate, value);
        }

        public CommandIcon Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        public string IconDisplay => Icon switch
        {
            CommandIcon.Folder => "📁",
            CommandIcon.Web => "🌐",
            CommandIcon.Script => "⚙️",
            CommandIcon.Document => "📄",
            _ => "📁"
        };

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

    public class SourceConfig : INotifyPropertyChanged
    {
        private string _path = string.Empty;
        private bool _recursive = true;
        private string _glob = "*.*";
        private List<string> _items = new List<string>();

        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        public bool Recursive
        {
            get => _recursive;
            set => SetProperty(ref _recursive, value);
        }

        public string Glob
        {
            get => _glob;
            set => SetProperty(ref _glob, value);
        }

        public List<string> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
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

    public class CommandResultItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string IconDisplay { get; set; } = string.Empty;
    }
}
