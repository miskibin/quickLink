using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace quickLink.Models
{
    public class ClipboardItem : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private string _value = string.Empty;
    private bool _isEncrypted;
     private bool _isLink;
        private bool _isCommand;
        private bool _isInternalCommand;

        public string Title
        {
get => _title;
  set => SetProperty(ref _title, value);
        }

        public string Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    // Auto-detect if it's a link
                    var newIsLink = !string.IsNullOrWhiteSpace(value) && 
                        (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                         value.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
                    
                    // Auto-detect if it's a command
                    var newIsCommand = !string.IsNullOrWhiteSpace(value) && value.StartsWith(">");
                    
                    // Batch property updates to minimize notifications
                    var linkChanged = _isLink != newIsLink;
                    var commandChanged = _isCommand != newIsCommand;
                    
                    if (linkChanged)
                    {
                        _isLink = newIsLink;
                        OnPropertyChanged(nameof(IsLink));
                    }
                    
                    if (commandChanged)
                    {
                        _isCommand = newIsCommand;
                        OnPropertyChanged(nameof(IsCommand));
                    }
                    
                    // Only notify dependent properties if something changed
                    if (linkChanged || commandChanged)
                    {
                        OnPropertyChanged(nameof(IsEncryptedAndNotLink));
                        OnPropertyChanged(nameof(IsPlainText));
                        OnPropertyChanged(nameof(IsCommandAndNotLink));
                    }
                    
                    OnPropertyChanged(nameof(DisplayValue));
                }
            }
        }      public bool IsEncrypted
        {
        get => _isEncrypted;
            set 
            {
                if (SetProperty(ref _isEncrypted, value))
                {
                    OnPropertyChanged(nameof(DisplayValue));
                    OnPropertyChanged(nameof(IsEncryptedAndNotLink));
                    OnPropertyChanged(nameof(IsPlainText));
                }
            }
        }

        public bool IsLink
        {
            get => _isLink;
            set 
            {
                if (SetProperty(ref _isLink, value))
                {
                    // Batch notifications for dependent properties
                    OnPropertyChanged(nameof(IsEncryptedAndNotLink));
                    OnPropertyChanged(nameof(IsPlainText));
                    OnPropertyChanged(nameof(IsCommandAndNotLink));
                }
            }
        }

        public bool IsCommand
        {
            get => _isCommand;
            set
            {
                if (SetProperty(ref _isCommand, value))
                {
                    // Batch notifications for dependent properties
                    OnPropertyChanged(nameof(IsCommandAndNotLink));
                    OnPropertyChanged(nameof(IsPlainText));
                }
            }
        }

        public bool IsInternalCommand
        {
            get => _isInternalCommand;
            set
            {
                if (SetProperty(ref _isInternalCommand, value))
                {
                    // Batch notifications for dependent properties
                    OnPropertyChanged(nameof(IsPlainText));
                    OnPropertyChanged(nameof(IsCommandAndNotLink));
                }
            }
        }

        public string DisplayTitle => string.IsNullOrWhiteSpace(Title) 
            ? (IsLink ? "🔗 Link" : IsCommand ? "⚡ Command" : "📄 Text") 
            : Title;

        public string DisplayValue
        {
            get
            {
                if (IsEncrypted) return "••••••••";
                // For commands (including internal command items), trim the > prefix for display
                if ((IsCommand || IsInternalCommand) && Value.StartsWith(">"))
                {
                    var trimmed = Value.Substring(1).Trim();
                    return string.IsNullOrWhiteSpace(trimmed) ? "..." : trimmed;
                }
                return Value;
            }
        }
        
        // Helper properties for icon visibility
        public bool IsEncryptedAndNotLink => IsEncrypted && !IsLink;
        public bool IsCommandAndNotLink => IsCommand && !IsLink && !IsInternalCommand;
        public bool IsPlainText => !IsEncrypted && !IsLink && !IsCommand && !IsInternalCommand;

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
