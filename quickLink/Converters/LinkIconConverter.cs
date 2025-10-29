using System;
using Microsoft.UI.Xaml.Data;

namespace quickLink.Converters
{
    public class LinkIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isLink)
            {
                // Return link icon for URLs, document icon for text
                return isLink ? "\uE71B" : "\uE8A5"; // E71B = Link, E8A5 = Document
            }
            return "\uE8A5";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class LinkIconGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isLink)
            {
                // Return Unicode glyph strings
                return isLink ? "&#xE71B;" : "&#xE8A5;";
            }
            return "&#xE8A5;";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
