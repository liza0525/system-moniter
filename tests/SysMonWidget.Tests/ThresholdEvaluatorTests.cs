using SysMonWidget.Models;
using SysMonWidget.Services;
using Xunit;

namespace SysMonWidget.Tests;

public class ThresholdEvaluatorTests
{
    private readonly ThresholdEvaluator _sut = new();
    private readonly MetricThreshold _threshold = new() { WarningPercent = 70, CriticalPercent = 90 };

    [Fact]
    public void Evaluate_BelowWarning_ReturnsNormal()
    {
        var result = _sut.Evaluate(50, _threshold);
        Assert.Equal(MetricStatus.Normal, result);
    }

    [Fact]
    public void Evaluate_AtWarningThreshold_ReturnsWarning()
    {
        var result = _sut.Evaluate(70, _threshold);
        Assert.Equal(MetricStatus.Warning, result);
    }

    [Fact]
    public void Evaluate_BetweenWarningAndCritical_ReturnsWarning()
    {
        var result = _sut.Evaluate(85, _threshold);
        Assert.Equal(MetricStatus.Warning, result);
    }

    [Fact]
    public void Evaluate_AtCriticalThreshold_ReturnsCritical()
    {
        var result = _sut.Evaluate(90, _threshold);
        Assert.Equal(MetricStatus.Critical, result);
    }
}
