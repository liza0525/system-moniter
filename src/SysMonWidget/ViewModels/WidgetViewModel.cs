using System.ComponentModel;
using SysMonWidget.Models;
using SysMonWidget.Services;

namespace SysMonWidget.ViewModels;

public class WidgetViewModel : INotifyPropertyChanged
{
    private readonly ThresholdEvaluator _evaluator;
    private ThresholdSettings _thresholds;

    public event PropertyChangedEventHandler? PropertyChanged;

    public double CpuUsagePercent { get; private set; }
    public MetricStatus CpuStatus { get; private set; }
    public double MemoryUsagePercent { get; private set; }
    public MetricStatus MemoryStatus { get; private set; }
    public double GpuUsagePercent { get; private set; }
    public MetricStatus GpuStatus { get; private set; }
    public double DiskUsagePercent { get; private set; }
    public MetricStatus DiskStatus { get; private set; }
    public double NetworkUsagePercent { get; private set; }
    public MetricStatus NetworkStatus { get; private set; }

    public WidgetViewModel(ThresholdEvaluator evaluator, ThresholdSettings thresholds)
    {
        _evaluator = evaluator;
        _thresholds = thresholds;
    }

    public void UpdateThresholds(ThresholdSettings thresholds) => _thresholds = thresholds;

    public void UpdateFromSnapshot(MetricSnapshot snapshot)
    {
        CpuUsagePercent = snapshot.CpuUsagePercent;
        CpuStatus = _evaluator.Evaluate(snapshot.CpuUsagePercent, _thresholds.Cpu);

        MemoryUsagePercent = snapshot.MemoryUsagePercent;
        MemoryStatus = _evaluator.Evaluate(snapshot.MemoryUsagePercent, _thresholds.Memory);

        GpuUsagePercent = snapshot.GpuUsagePercent;
        GpuStatus = _evaluator.Evaluate(snapshot.GpuUsagePercent, _thresholds.Gpu);

        DiskUsagePercent = snapshot.DiskActiveTimePercent;
        DiskStatus = _evaluator.Evaluate(snapshot.DiskActiveTimePercent, _thresholds.Disk);

        NetworkUsagePercent = snapshot.NetworkUtilizationPercent;
        NetworkStatus = _evaluator.Evaluate(snapshot.NetworkUtilizationPercent, _thresholds.Network);

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }
}
