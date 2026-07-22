using System.Diagnostics;

namespace SysMonWidget.Services;

public class GpuMetricsProvider : IMetricsProvider
{
    private const string CategoryName = "GPU Engine";
    private const string CounterName = "Utilization Percentage";

    public double GetCurrentValue()
    {
        if (!PerformanceCounterCategory.Exists(CategoryName))
        {
            return 0;
        }

        var category = new PerformanceCounterCategory(CategoryName);
        var instanceNames = category.GetInstanceNames();
        var counters = new List<(string InstanceName, PerformanceCounter Counter)>();

        foreach (var name in instanceNames)
        {
            try
            {
                counters.Add((name, new PerformanceCounter(CategoryName, CounterName, name, readOnly: true)));
            }
            catch (InvalidOperationException)
            {
                // "GPU Engine" 인스턴스는 프로세스별로 동적으로 생겼다 사라진다.
                // 목록을 가져온 직후 해당 프로세스가 종료되면 생성 시점에 이미 없을 수 있다 — 건너뛴다.
            }
        }

        try
        {
            // 카운터마다 개별적으로 sleep하면 인스턴스 수에 비례해 지연이 커지므로,
            // 전체 워밍업 후 한 번만 대기한다.
            foreach (var (_, counter) in counters)
            {
                TryNextValue(counter);
            }

            Thread.Sleep(50);

            var engineTotals = new Dictionary<string, double>();
            foreach (var (instanceName, counter) in counters)
            {
                var value = TryNextValue(counter);
                if (value is null) continue; // 두 번째 샘플 시점에 인스턴스가 사라짐 — 건너뛴다.

                var engineType = ExtractEngineType(instanceName);
                engineTotals.TryGetValue(engineType, out var current);
                engineTotals[engineType] = current + value.Value;
            }

            return engineTotals.Count == 0 ? 0 : Math.Clamp(engineTotals.Values.Max(), 0, 100);
        }
        finally
        {
            foreach (var (_, counter) in counters)
            {
                counter.Dispose();
            }
        }
    }

    // GPU Engine 인스턴스는 읽는 도중에도 사라질 수 있어(프로세스 종료 등),
    // 이 경우 InvalidOperationException을 던지므로 해당 샘플만 건너뛴다.
    private static double? TryNextValue(PerformanceCounter counter)
    {
        try
        {
            return counter.NextValue();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static string ExtractEngineType(string instanceName)
    {
        var index = instanceName.IndexOf("engtype_", StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? instanceName[index..] : instanceName;
    }
}
