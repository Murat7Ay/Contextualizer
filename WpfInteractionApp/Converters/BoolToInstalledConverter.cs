using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfInteractionApp.Converters
{
    public class BoolToInstalledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "Installed" : "Not Installed";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 