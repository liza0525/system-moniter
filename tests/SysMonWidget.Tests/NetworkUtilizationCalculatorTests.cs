using SysMonWidget.Services;
using Xunit;

namespace SysMonWidget.Tests;

public class NetworkUtilizationCalculatorTests
{
    [Fact]
    public void CalculatePercent_HalfOfLinkSpeed_Returns50()
    {
        // 1000 Mbps 링크, 초당 62.5MB(=500Mbit) 전송 중
        var result = NetworkUtilizationCalculator.CalculatePercent(
            bytesPerSecond: 62_500_000, linkSpeedBitsPerSecond: 1_000_000_000);

        Assert.Equal(50, result, precision: 1);
    }

    [Fact]
    public void CalculatePercent_ZeroLinkSpeed_ReturnsZero()
    {
        var result = NetworkUtilizationCalculator.CalculatePercent(1000, 0);
        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculatePercent_ExceedsLinkSpeed_ClampsTo100()
    {
        var result = NetworkUtilizationCalculator.CalculatePercent(
            bytesPerSecond: 200_000_000, linkSpeedBitsPerSecond: 1_000_000_000);

        Assert.Equal(100, result);
    }
}
