using System.Diagnostics;

namespace SysMonWidget.Services;

public class CpuMetricsProvider : IMetricsProvider, IDisposable
{
    private readonly PerformanceCounter _counter = new("Processor", "% Processor Time", "_Total");

    public CpuMetricsProvider()
    {
        _counter.NextValue(); // 첫 호출은 항상 0 — 워밍업
    }

    public double GetCurrentValue() => Math.Clamp(_counter.NextValue(), 0, 100);

    public void Dispose() => _counter.Dispose();
}
