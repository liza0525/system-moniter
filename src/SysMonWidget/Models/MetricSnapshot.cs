namespace SysMonWidget.Models;

public record MetricSnapshot(
    double CpuUsagePercent,
    double MemoryUsagePercent,
    double GpuUsagePercent,
    double DiskActiveTimePercent,
    double NetworkUtilizationPercent,
    DateTime Timestamp);
