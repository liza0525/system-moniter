namespace SysMonWidget.Services;

public static class NetworkUtilizationCalculator
{
    public static double CalculatePercent(double bytesPerSecond, double linkSpeedBitsPerSecond)
    {
        if (linkSpeedBitsPerSecond <= 0) return 0;

        var bitsPerSecond = bytesPerSecond * 8;
        var percent = bitsPerSecond / linkSpeedBitsPerSecond * 100;
        return Math.Clamp(percent, 0, 100);
    }
}
