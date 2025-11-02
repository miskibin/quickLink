using System;
using Microsoft.UI.Xaml.Data;

namespace quickLink.Converters
{
    public class InternalCommandIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string commandValue)
            {
                return commandValue switch
                {
                    "internal:add" => "\uE710",      // Add icon
                    "internal:settings" => "\uE713", // Settings gear icon
                    "internal:exit" => "\uE711",     // Close/Exit icon
                    ">next" => "\uE893",             // Next track icon
                    ">prev" => "\uE892",             // Previous track icon
                    ">playpause" => "\uE768",        // Play/Pause icon
                    _ => "\uE8B7"                    // Default bulleted list icon
                };
            }
            return "\uE8B7"; // Default
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
