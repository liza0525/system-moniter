using System.Globalization;
using System.Windows.Data;
using SysMonWidget.Models;
using Brushes = System.Windows.Media.Brushes;

namespace SysMonWidget.Converters;

public class MetricStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (MetricStatus)value switch
        {
            MetricStatus.Critical => Brushes.Red,
            MetricStatus.Warning => Brushes.Gold,
            _ => Brushes.LightGreen,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
