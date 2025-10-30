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
   IsLink = !string.IsNullOrWhiteSpace(value) && 
      (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
           value.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
                    
                    // Auto-detect if it's a command
                    IsCommand = !string.IsNullOrWhiteSpace(value) && value.StartsWith(">");
                    
                    OnPropertyChanged(nameof(DisplayValue));
       }
            }
        }

      public bool IsEncrypted
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
                    OnPropertyChanged(nameof(IsCommandAndNotLink));
                    OnPropertyChanged(nameof(IsPlainText));
                }
            }
        }

        public string DisplayTitle => string.IsNullOrWhiteSpace(Title) 
      ? (IsLink ? "🔗 Link" : IsCommand ? "⚡ Command" : "📄 Text") 
            : Title;

        public string DisplayValue => IsEncrypted ? "••••••••" : Value;

        // Helper properties for icon visibility
        public bool IsEncryptedAndNotLink => IsEncrypted && !IsLink;
        public bool IsCommandAndNotLink => IsCommand && !IsLink;
        public bool IsPlainText => !IsEncrypted && !IsLink && !IsCommand;

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
