using SysMonWidget.Models;
using SysMonWidget.Services;
using Xunit;

namespace SysMonWidget.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempFile =
        Path.Combine(Path.GetTempPath(), $"sysmon-test-{Guid.NewGuid()}.json");

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsDefaultSettings()
    {
        var sut = new SettingsService(_tempFile);

        var result = sut.Load();

        Assert.Equal(70, result.Thresholds.Cpu.WarningPercent);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsValues()
    {
        var sut = new SettingsService(_tempFile);
        var settings = new AppSettings { WindowLeft = 250, Opacity = 0.5, RunAtStartup = true };

        sut.Save(settings);
        var loaded = sut.Load();

        Assert.Equal(250, loaded.WindowLeft);
        Assert.Equal(0.5, loaded.Opacity);
        Assert.True(loaded.RunAtStartup);
    }

    [Fact]
    public void SettingsFileExists_WhenFileDoesNotExist_ReturnsFalse()
    {
        var sut = new SettingsService(_tempFile);

        Assert.False(sut.SettingsFileExists());
    }

    [Fact]
    public void SettingsFileExists_AfterSave_ReturnsTrue()
    {
        var sut = new SettingsService(_tempFile);
        sut.Save(new AppSettings());

        Assert.True(sut.SettingsFileExists());
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }
}
