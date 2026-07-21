using SysMonWidget.Models;

namespace SysMonWidget.Services;

public class MetricsAggregator
{
    private readonly IMetricsProvider _cpu;
    private readonly IMetricsProvider _memory;
    private readonly IMetricsProvider _gpu;
    private readonly IMetricsProvider _disk;
    private readonly IMetricsProvider _network;

    public MetricsAggregator(
        IMetricsProvider cpu,
        IMetricsProvider memory,
        IMetricsProvider gpu,
        IMetricsProvider disk,
        IMetricsProvider network)
    {
        _cpu = cpu;
        _memory = memory;
        _gpu = gpu;
        _disk = disk;
        _network = network;
    }

    public MetricSnapshot CollectSnapshot()
    {
        return new MetricSnapshot(
            _cpu.GetCurrentValue(),
            _memory.GetCurrentValue(),
            _gpu.GetCurrentValue(),
            _disk.GetCurrentValue(),
            _network.GetCurrentValue(),
            DateTime.Now);
    }
}
