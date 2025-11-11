using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace quickLink.Converters
{
    public class SafeImageConverter : IValueConverter
    {
        // Simple in-memory cache for bitmap images
        private static readonly Dictionary<string, BitmapImage> _imageCache = new();
        private static readonly object _cacheLock = new();

        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string url && !string.IsNullOrEmpty(url))
            {
                try
                {
                    // Check cache first
                    lock (_cacheLock)
                    {
                        if (_imageCache.TryGetValue(url, out var cachedImage))
                        {
                            return cachedImage;
                        }
                    }

                    // Create new bitmap with optimized settings
                    var bitmapImage = new BitmapImage();

                    // Decode image at smaller size to save memory (favicons are small anyway)
                    bitmapImage.DecodePixelWidth = 32;
                    bitmapImage.DecodePixelHeight = 32;

                    // Enable async loading to prevent UI blocking
                    bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;

                    bitmapImage.UriSource = new Uri(url, UriKind.Absolute);

                    // Cache the image for reuse
                    lock (_cacheLock)
                    {
                        // Limit cache size to prevent memory issues
                        if (_imageCache.Count > 100)
                        {
                            _imageCache.Clear(); // Simple eviction strategy
                        }

                        if (!_imageCache.ContainsKey(url))
                        {
                            _imageCache[url] = bitmapImage;
                        }
                    }

                    return bitmapImage;
                }
                catch
                {
                    return null; // Return null for failed loads
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
