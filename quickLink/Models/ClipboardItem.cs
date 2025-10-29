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
       }
            }
        }

      public bool IsEncrypted
        {
        get => _isEncrypted;
            set => SetProperty(ref _isEncrypted, value);
        }

        public bool IsLink
    {
       get => _isLink;
     set => SetProperty(ref _isLink, value);
      }

        public string DisplayTitle => string.IsNullOrWhiteSpace(Title) 
      ? (IsLink ? "?? Link" : "?? Text") 
            : Title;

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
