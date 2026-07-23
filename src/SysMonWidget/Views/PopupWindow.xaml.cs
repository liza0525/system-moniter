using System.Windows;
using System.Windows.Threading;
using SysMonWidget.Models;
using SysMonWidget.Services;
using SysMonWidget.ViewModels;

namespace SysMonWidget.Views;

public partial class PopupWindow : Window
{
    private readonly WidgetViewModel _viewModel;
    private readonly ThresholdSettings _thresholds;
    private readonly DispatcherTimer _saveConfirmationTimer;

    public event EventHandler<ThresholdSettings>? ThresholdsSaved;
    public event EventHandler<double>? OpacityChanged;

    public PopupWindow(WidgetViewModel viewModel, ThresholdSettings thresholds, double currentOpacity)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _thresholds = thresholds;

        _saveConfirmationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _saveConfirmationTimer.Tick += (_, _) =>
        {
            _saveConfirmationTimer.Stop();
            SaveConfirmationPopup.IsOpen = false;
        };

        CpuWarningBox.Text = thresholds.Cpu.WarningPercent.ToString();
        CpuCriticalBox.Text = thresholds.Cpu.CriticalPercent.ToString();
        MemoryWarningBox.Text = thresholds.Memory.WarningPercent.ToString();
        MemoryCriticalBox.Text = thresholds.Memory.CriticalPercent.ToString();
        GpuWarningBox.Text = thresholds.Gpu.WarningPercent.ToString();
        GpuCriticalBox.Text = thresholds.Gpu.CriticalPercent.ToString();
        DiskWarningBox.Text = thresholds.Disk.WarningPercent.ToString();
        DiskCriticalBox.Text = thresholds.Disk.CriticalPercent.ToString();
        NetworkWarningBox.Text = thresholds.Network.WarningPercent.ToString();
        NetworkCriticalBox.Text = thresholds.Network.CriticalPercent.ToString();

        RunAtStartupCheckBox.IsChecked = new StartupRegistrationService(Environment.ProcessPath!).IsEnabled();

        OpacitySlider.Value = currentOpacity;
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        OpacityChanged?.Invoke(this, e.NewValue);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _thresholds.Cpu.WarningPercent = ParseOrDefault(CpuWarningBox.Text, _thresholds.Cpu.WarningPercent);
        _thresholds.Cpu.CriticalPercent = ParseOrDefault(CpuCriticalBox.Text, _thresholds.Cpu.CriticalPercent);
        _thresholds.Memory.WarningPercent = ParseOrDefault(MemoryWarningBox.Text, _thresholds.Memory.WarningPercent);
        _thresholds.Memory.CriticalPercent = ParseOrDefault(MemoryCriticalBox.Text, _thresholds.Memory.CriticalPercent);
        _thresholds.Gpu.WarningPercent = ParseOrDefault(GpuWarningBox.Text, _thresholds.Gpu.WarningPercent);
        _thresholds.Gpu.CriticalPercent = ParseOrDefault(GpuCriticalBox.Text, _thresholds.Gpu.CriticalPercent);
        _thresholds.Disk.WarningPercent = ParseOrDefault(DiskWarningBox.Text, _thresholds.Disk.WarningPercent);
        _thresholds.Disk.CriticalPercent = ParseOrDefault(DiskCriticalBox.Text, _thresholds.Disk.CriticalPercent);
        _thresholds.Network.WarningPercent = ParseOrDefault(NetworkWarningBox.Text, _thresholds.Network.WarningPercent);
        _thresholds.Network.CriticalPercent = ParseOrDefault(NetworkCriticalBox.Text, _thresholds.Network.CriticalPercent);

        _viewModel.UpdateThresholds(_thresholds);

        new StartupRegistrationService(Environment.ProcessPath!)
            .SetEnabled(RunAtStartupCheckBox.IsChecked == true);

        ThresholdsSaved?.Invoke(this, _thresholds);

        _saveConfirmationTimer.Stop();
        SaveConfirmationPopup.IsOpen = true;
        _saveConfirmationTimer.Start();
    }

    private static double ParseOrDefault(string text, double fallback) =>
        double.TryParse(text, out var value) ? value : fallback;
}
