using System.Globalization;
using System.Windows.Data;
using SysMonWidget.Models;
using Application = System.Windows.Application;

namespace SysMonWidget.Converters;

public class MetricStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var suffix = string.Equals(parameter as string, "Light", StringComparison.OrdinalIgnoreCase)
            ? "Light"
            : "Dark";

        var key = (MetricStatus)value switch
        {
            MetricStatus.Critical => $"StatusCritical{suffix}Brush",
            MetricStatus.Warning => $"StatusWarning{suffix}Brush",
            _ => $"StatusNormal{suffix}Brush",
        };

        return Application.Current.Resources[key];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
