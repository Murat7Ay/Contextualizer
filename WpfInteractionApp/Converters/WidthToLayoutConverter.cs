using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfInteractionApp.Converters
{
    public class WidthToLayoutConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                // Define breakpoints
                if (width < 800)
                    return "Compact";
                else if (width < 1200)
                    return "Medium";
                else
                    return "Wide";
            }
            
            return "Medium";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
