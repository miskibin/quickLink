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
                    OnPropertyChanged(nameof(IconGlyph));
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
             OnPropertyChanged(nameof(IconGlyph));
         }
     }
      }

        public string DisplayTitle => string.IsNullOrWhiteSpace(Title) 
      ? (IsLink ? "🔗 Link" : "📄 Text") 
            : Title;

        public string DisplayValue => IsEncrypted ? "••••••••" : Value;

        public string IconGlyph => IsEncrypted ? "\uE72E" : (IsLink ? "\uE71B" : "\uE8A5");

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
