using SysMonWidget.Models;
using SysMonWidget.Services;
using SysMonWidget.ViewModels;
using Xunit;

namespace SysMonWidget.Tests;

public class WidgetViewModelTests
{
    [Fact]
    public void UpdateFromSnapshot_CpuAboveCritical_SetsCriticalStatus()
    {
        var sut = new WidgetViewModel(new ThresholdEvaluator(), new ThresholdSettings());
        var snapshot = new MetricSnapshot(95, 10, 10, 10, 10, DateTime.Now);

        sut.UpdateFromSnapshot(snapshot);

        Assert.Equal(MetricStatus.Critical, sut.CpuStatus);
        Assert.Equal(95, sut.CpuUsagePercent);
    }

    [Fact]
    public void UpdateFromSnapshot_AllBelowWarning_AllNormal()
    {
        var sut = new WidgetViewModel(new ThresholdEvaluator(), new ThresholdSettings());
        var snapshot = new MetricSnapshot(10, 10, 10, 10, 10, DateTime.Now);

        sut.UpdateFromSnapshot(snapshot);

        Assert.Equal(MetricStatus.Normal, sut.CpuStatus);
        Assert.Equal(MetricStatus.Normal, sut.MemoryStatus);
        Assert.Equal(MetricStatus.Normal, sut.GpuStatus);
        Assert.Equal(MetricStatus.Normal, sut.DiskStatus);
        Assert.Equal(MetricStatus.Normal, sut.NetworkStatus);
    }

    [Fact]
    public void UpdateFromSnapshot_RaisesPropertyChanged()
    {
        var sut = new WidgetViewModel(new ThresholdEvaluator(), new ThresholdSettings());
        var raised = false;
        sut.PropertyChanged += (_, _) => raised = true;

        sut.UpdateFromSnapshot(new MetricSnapshot(10, 10, 10, 10, 10, DateTime.Now));

        Assert.True(raised);
    }
}
