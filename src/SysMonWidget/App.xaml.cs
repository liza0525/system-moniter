using System.Windows;
using System.Windows.Threading;
using SysMonWidget.Models;
using SysMonWidget.Services;
using SysMonWidget.ViewModels;
using SysMonWidget.Views;
using Application = System.Windows.Application;

namespace SysMonWidget;

public partial class App : Application
{
    private DispatcherTimer? _timer;
    private MetricsAggregator? _aggregator;
    private WidgetViewModel? _viewModel;
    private SettingsService? _settingsService;
    private AppSettings? _appSettings;
    private WidgetWindow? _widgetWindow;
    private TrayIconManager? _trayIconManager;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsService = new SettingsService();
        _appSettings = _settingsService.Load();

        _aggregator = new MetricsAggregator(
            cpu: new CpuMetricsProvider(),
            memory: new MemoryMetricsProvider(),
            gpu: new GpuMetricsProvider(),
            disk: new DiskMetricsProvider(),
            network: new NetworkMetricsProvider());

        _viewModel = new WidgetViewModel(new ThresholdEvaluator(), _appSettings.Thresholds);

        _widgetWindow = new WidgetWindow(_viewModel);
        _widgetWindow.ApplySettings(_appSettings);
        _widgetWindow.TogglePopupRequested += OnTogglePopupRequested;
        _widgetWindow.Show();

        _trayIconManager = new TrayIconManager(_widgetWindow);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += async (_, _) =>
        {
            var snapshot = await Task.Run(() => _aggregator.CollectSnapshot());
            _viewModel.UpdateFromSnapshot(snapshot);
        };
        _timer.Start();
    }

    private void OnTogglePopupRequested(object? sender, EventArgs e)
    {
        var popup = new PopupWindow(_viewModel!, _appSettings!.Thresholds);
        popup.ThresholdsSaved += (_, updatedThresholds) =>
        {
            _appSettings.Thresholds = updatedThresholds;
            _settingsService!.Save(_appSettings);
        };
        popup.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _timer?.Stop();

        if (_widgetWindow is not null && _appSettings is not null && _settingsService is not null)
        {
            _appSettings.WindowLeft = _widgetWindow.CurrentPosition.Left;
            _appSettings.WindowTop = _widgetWindow.CurrentPosition.Top;
            _appSettings.Opacity = _widgetWindow.Opacity;
            _settingsService.Save(_appSettings);
        }

        _trayIconManager?.Dispose();

        base.OnExit(e);
    }
}
