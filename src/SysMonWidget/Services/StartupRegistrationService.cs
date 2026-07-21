using Microsoft.Win32;

namespace SysMonWidget.Services;

public class StartupRegistrationService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SysMonWidget";
    private readonly string _executablePath;

    public StartupRegistrationService(string executablePath)
    {
        _executablePath = executablePath;
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

        if (enabled)
        {
            key!.SetValue(ValueName, $"\"{_executablePath}\"");
        }
        else
        {
            key!.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) != null;
    }
}
