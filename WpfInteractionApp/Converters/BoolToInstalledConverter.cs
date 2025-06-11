using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfInteractionApp.Converters
{
    public class BoolToInstalledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isInstalled)
            {
                return isInstalled ? "Yüklü" : "Yüklü Değil";
            }
            return "Yüklü Değil";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return str == "Yüklü";
            }
            return false;
        }
    }
} 