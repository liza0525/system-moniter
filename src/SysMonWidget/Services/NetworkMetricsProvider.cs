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

        // "Bytes Total/sec"는 두 샘플 사이의 델타로 계산되는 속도(rate) 카운터라
        // 생성 직후 첫 NextValue() 호출은 항상 0을 반환한다. 매번 카운터를 새로
        // 만드는 구조이므로, 워밍업 호출 후 잠깐 대기했다가 실제 값을 읽어야 한다.
        var activeInstances = new List<(PerformanceCounter Bytes, double Bandwidth)>();

        foreach (var instanceName in instanceNames)
        {
            using var bandwidthCounter = new PerformanceCounter(CategoryName, "Current Bandwidth", instanceName, readOnly: true);
            var bandwidth = bandwidthCounter.NextValue(); // 순간값이라 워밍업 불필요

            if (bandwidth <= 0) continue; // 비활성 NIC 제외

            var bytesCounter = new PerformanceCounter(CategoryName, "Bytes Total/sec", instanceName, readOnly: true);
            bytesCounter.NextValue(); // 워밍업 — 이 값은 버린다
            activeInstances.Add((bytesCounter, bandwidth));
        }

        try
        {
            if (activeInstances.Count == 0) return 0;

            Thread.Sleep(50);

            double totalBytesPerSecond = 0;
            double totalBandwidthBits = 0;
            foreach (var (bytesCounter, bandwidth) in activeInstances)
            {
                totalBytesPerSecond += bytesCounter.NextValue();
                totalBandwidthBits += bandwidth;
            }

            return NetworkUtilizationCalculator.CalculatePercent(totalBytesPerSecond, totalBandwidthBits);
        }
        finally
        {
            foreach (var (bytesCounter, _) in activeInstances)
            {
                bytesCounter.Dispose();
            }
        }
    }
}
