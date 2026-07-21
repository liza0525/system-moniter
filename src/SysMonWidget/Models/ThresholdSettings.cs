namespace SysMonWidget.Models;

public class MetricThreshold
{
    public double WarningPercent { get; set; }
    public double CriticalPercent { get; set; }
}

public class ThresholdSettings
{
    public MetricThreshold Cpu { get; set; } = new() { WarningPercent = 70, CriticalPercent = 90 };
    public MetricThreshold Memory { get; set; } = new() { WarningPercent = 80, CriticalPercent = 90 };
    public MetricThreshold Gpu { get; set; } = new() { WarningPercent = 80, CriticalPercent = 90 };
    public MetricThreshold Disk { get; set; } = new() { WarningPercent = 70, CriticalPercent = 90 };
    public MetricThreshold Network { get; set; } = new() { WarningPercent = 70, CriticalPercent = 90 };
}

public enum MetricStatus
{
    Normal,
    Warning,
    Critical
}
