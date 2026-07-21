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
        var counters = instanceNames
            .Select(name => new PerformanceCounter(CategoryName, CounterName, name, readOnly: true))
            .ToList();

        try
        {
            // 카운터마다 개별적으로 sleep하면 인스턴스 수에 비례해 지연이 커지므로,
            // 전체 워밍업 후 한 번만 대기한다.
            foreach (var counter in counters)
            {
                counter.NextValue();
            }

            Thread.Sleep(50);

            var engineTotals = new Dictionary<string, double>();
            for (var i = 0; i < counters.Count; i++)
            {
                var value = counters[i].NextValue();
                var engineType = ExtractEngineType(instanceNames[i]);
                engineTotals.TryGetValue(engineType, out var current);
                engineTotals[engineType] = current + value;
            }

            return engineTotals.Count == 0 ? 0 : Math.Clamp(engineTotals.Values.Max(), 0, 100);
        }
        finally
        {
            foreach (var counter in counters)
            {
                counter.Dispose();
            }
        }
    }

    private static string ExtractEngineType(string instanceName)
    {
        var index = instanceName.IndexOf("engtype_", StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? instanceName[index..] : instanceName;
    }
}
