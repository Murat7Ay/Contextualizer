using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfInteractionApp.Converters
{
    public class StatusToColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is bool enabled)
            {
                return enabled ? Colors.Green : Colors.Red;
            }
            return Colors.Gray;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}