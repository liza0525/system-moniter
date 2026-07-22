using System.IO;
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
    private bool _isCollecting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;

        _settingsService = new SettingsService();
        var isFirstLaunch = !_settingsService.SettingsFileExists();
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

        _trayIconManager = new TrayIconManager(_widgetWindow, isFirstLaunch);
        _trayIconManager.OpenSettingsRequested += OnTogglePopupRequested;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += async (_, _) =>
        {
            // 이전 tick의 수집이 아직 진행 중이면 이번 tick은 건너뛴다.
            // 겹쳐서 실행되면 CPU/디스크 프로바이더가 재사용하는 PerformanceCounter를
            // 여러 스레드가 동시에 건드리게 되어 델타 계산이 깨진다 (스레드 안전하지 않음).
            if (_isCollecting) return;
            _isCollecting = true;
            try
            {
                var snapshot = await Task.Run(() => _aggregator.CollectSnapshot());
                _viewModel.UpdateFromSnapshot(snapshot);
            }
            finally
            {
                _isCollecting = false;
            }
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

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception);
        e.Handled = true; // 위젯이 통째로 죽는 대신 계속 동작하도록 함 (해당 tick의 갱신만 건너뜀)
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogException(ex);
        }
    }

    private static void LogException(Exception ex)
    {
        try
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SysMonWidget", "crash.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n");
        }
        catch
        {
            // 로그 기록 자체가 실패해도 앱이 죽으면 안 되므로 무시한다.
        }
    }
}
