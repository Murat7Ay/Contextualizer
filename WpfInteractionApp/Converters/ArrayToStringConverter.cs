using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace WpfInteractionApp.Converters
{
    public class ArrayToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string[] array)
            {
                return string.Join(", ", array);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return str.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }
            return Array.Empty<string>();
        }
    }
} 