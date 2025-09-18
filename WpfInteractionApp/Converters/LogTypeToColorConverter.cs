using Contextualizer.PluginContracts;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfInteractionApp.Converters
{
    public class LogTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LogType logType)
            {
                return logType switch
                {
                    LogType.Success => new SolidColorBrush(Color.FromRgb(34, 197, 94)),   // Green
                    LogType.Info => new SolidColorBrush(Color.FromRgb(59, 130, 246)),     // Blue
                    LogType.Warning => new SolidColorBrush(Color.FromRgb(245, 158, 11)),  // Orange
                    LogType.Error => new SolidColorBrush(Color.FromRgb(239, 68, 68)),     // Red
                    LogType.Critical => new SolidColorBrush(Color.FromRgb(147, 51, 234)), // Purple
                    LogType.Debug => new SolidColorBrush(Color.FromRgb(107, 114, 128)),   // Gray
                    LogType.Trace => new SolidColorBrush(Color.FromRgb(156, 163, 175)),   // Light Gray
                    _ => new SolidColorBrush(Color.FromRgb(107, 114, 128))                // Default Gray
                };
            }
            
            return new SolidColorBrush(Color.FromRgb(107, 114, 128)); // Default Gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
