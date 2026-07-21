namespace SysMonWidget.Models;

public class AppSettings
{
    public double WindowLeft { get; set; } = 100;
    public double WindowTop { get; set; } = 100;
    public double Opacity { get; set; } = 0.85;
    public bool RunAtStartup { get; set; } = false;
    public ThresholdSettings Thresholds { get; set; } = new();
}
