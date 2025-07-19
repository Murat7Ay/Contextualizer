using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfInteractionApp.Converters
{
    public class LastResultConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var lastError = values[0] as string;
            var lastExecution = values[1] as DateTime?;

            if (!string.IsNullOrEmpty(lastError))
                return "Error";
            
            if (lastExecution.HasValue)
                return "Success";
                
            return "—";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LastResultColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var lastError = values[0] as string;
            
            if (!string.IsNullOrEmpty(lastError))
                return new SolidColorBrush(Colors.Red);
                
            return new SolidColorBrush(Colors.Green);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnableButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "Disable" : "Enable";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnableButtonColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var enabled = (bool)value;
            return enabled 
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc3545")) // Red for disable
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28a745")); // Green for enable
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}