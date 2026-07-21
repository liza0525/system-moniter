using SysMonWidget.Services;
using Xunit;

namespace SysMonWidget.Tests;

public class MetricsAggregatorTests
{
    private class FakeProvider : IMetricsProvider
    {
        private readonly double _value;
        public FakeProvider(double value) => _value = value;
        public double GetCurrentValue() => _value;
    }

    [Fact]
    public void CollectSnapshot_ReturnsValuesFromEachProvider()
    {
        var sut = new MetricsAggregator(
            cpu: new FakeProvider(10),
            memory: new FakeProvider(20),
            gpu: new FakeProvider(30),
            disk: new FakeProvider(40),
            network: new FakeProvider(50));

        var snapshot = sut.CollectSnapshot();

        Assert.Equal(10, snapshot.CpuUsagePercent);
        Assert.Equal(20, snapshot.MemoryUsagePercent);
        Assert.Equal(30, snapshot.GpuUsagePercent);
        Assert.Equal(40, snapshot.DiskActiveTimePercent);
        Assert.Equal(50, snapshot.NetworkUtilizationPercent);
    }
}
