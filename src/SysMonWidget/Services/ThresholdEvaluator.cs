using SysMonWidget.Models;

namespace SysMonWidget.Services;

public class ThresholdEvaluator
{
    public MetricStatus Evaluate(double value, MetricThreshold threshold)
    {
        if (value >= threshold.CriticalPercent) return MetricStatus.Critical;
        if (value >= threshold.WarningPercent) return MetricStatus.Warning;
        return MetricStatus.Normal;
    }
}
