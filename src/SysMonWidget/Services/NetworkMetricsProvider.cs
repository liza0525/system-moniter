using System.Diagnostics;

namespace SysMonWidget.Services;

public class NetworkMetricsProvider : IMetricsProvider
{
    private const string CategoryName = "Network Interface";

    public double GetCurrentValue()
    {
        if (!PerformanceCounterCategory.Exists(CategoryName))
        {
            return 0;
        }

        var category = new PerformanceCounterCategory(CategoryName);
        var instanceNames = category.GetInstanceNames();

        double totalBytesPerSecond = 0;
        double totalBandwidthBits = 0;

        foreach (var instanceName in instanceNames)
        {
            using var bytesCounter = new PerformanceCounter(CategoryName, "Bytes Total/sec", instanceName, readOnly: true);
            using var bandwidthCounter = new PerformanceCounter(CategoryName, "Current Bandwidth", instanceName, readOnly: true);

            var bandwidth = bandwidthCounter.NextValue();
            if (bandwidth <= 0) continue; // 비활성 NIC 제외

            totalBytesPerSecond += bytesCounter.NextValue();
            totalBandwidthBits += bandwidth;
        }

        return NetworkUtilizationCalculator.CalculatePercent(totalBytesPerSecond, totalBandwidthBits);
    }
}
