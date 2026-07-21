using System.Diagnostics;

namespace SysMonWidget.Services;

public class DiskMetricsProvider : IMetricsProvider, IDisposable
{
    private readonly PerformanceCounter _counter = new("PhysicalDisk", "% Disk Time", "_Total");

    public DiskMetricsProvider()
    {
        _counter.NextValue();
    }

    public double GetCurrentValue() => Math.Clamp(_counter.NextValue(), 0, 100);

    public void Dispose() => _counter.Dispose();
}
