# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

SysMonWidget: a Windows always-on-top desktop widget (WPF, .NET 8) showing live CPU/RAM/GPU/disk/network usage, with a right-click popup for an expanded view and per-metric warning/critical threshold editing. Full requirements are in [PRD.md](PRD.md); the task-by-task build plan is in `docs/superpowers/plans/2026-07-21-sysmonwidget-implementation.md`.

## Platform requirement

This is a Windows-only WPF app (`net8.0-windows`, `UseWPF`/`UseWindowsForms`). It cannot be built or run on Linux/macOS.

If working from WSL against a Windows-side `.NET SDK` install, invoke the Windows `dotnet.exe` directly rather than the Linux one:
```
"/mnt/c/Program Files/dotnet/dotnet.exe" build
```
This resolves the WSL project path as `\\wsl.localhost\...` and works fine for `dotnet.exe` even though `cmd.exe` refuses UNC working directories.

## Commands

Always use forward slashes in `--project` paths, even on Windows — in bash-like shells (Git Bash, WSL) a backslash before a capital letter gets swallowed as an escape (e.g. `src\SysMonWidget` becomes `srcSysMonWidget`).

```
dotnet build
dotnet run --project src/SysMonWidget
dotnet test
dotnet test --filter ThresholdEvaluatorTests
dotnet test --filter "FullyQualifiedName~WidgetViewModelTests.UpdateFromSnapshot_RaisesPropertyChanged"
dotnet publish src/SysMonWidget -c Release
```
`dotnet run`/`dotnet test` rebuild automatically on source changes — no separate `dotnet build` needed, except that a currently-running `SysMonWidget.exe` will lock its own binary and must be closed first (tray icon → 종료, or Task Manager).

Publish output (self-contained single file): `src/SysMonWidget/bin/Release/net8.0-windows/win-x64/publish/SysMonWidget.exe`.

## Architecture

Runtime flow, once per second: `App`'s `DispatcherTimer` fires → `MetricsAggregator.CollectSnapshot()` runs on a background thread via `Task.Run` (must stay off the UI thread — see gotchas below) → the resulting `MetricSnapshot` is handed to `WidgetViewModel.UpdateFromSnapshot` back on the UI thread → `ThresholdEvaluator` classifies each metric against `ThresholdSettings` → `PropertyChanged` fires → `WidgetWindow`/`PopupWindow` bindings refresh (text + status color).

- **Models** (`src/SysMonWidget/Models`): plain data — `MetricSnapshot`, `ThresholdSettings`/`MetricThreshold`/`MetricStatus`, `AppSettings`.
- **Services** (`src/SysMonWidget/Services`): one `IMetricsProvider` implementation per metric (`Cpu`/`Memory`/`Gpu`/`Disk`/`NetworkMetricsProvider`), each returning a single value already normalized to 0–100. `MetricsAggregator` combines all five into a `MetricSnapshot`. `ThresholdEvaluator` is the only place that turns a raw value + threshold into `MetricStatus`. `NetworkUtilizationCalculator` converts bytes/sec + link speed into a percent (disk/network are normalized as "% of capacity", not raw throughput — see PRD). `SettingsService` persists `AppSettings` as JSON to `%AppData%\SysMonWidget\settings.json`. `StartupRegistrationService` toggles the `HKCU\...\Run` registry key.
- **ViewModels**: `WidgetViewModel` is the single `INotifyPropertyChanged` surface both windows bind to; `UpdateThresholds` lets the popup push edited thresholds back in without recreating the view model.
- **Views** (`src/SysMonWidget/Views`): `WidgetWindow` is the always-on-top mini widget (`Topmost`, `ShowInTaskbar=false`; left-click-drag calls `DragMove()`; right-click raises `TogglePopupRequested`). `PopupWindow` is the expanded view + threshold editor, raising `ThresholdsSaved` on save. `TrayIconManager` wraps a WinForms `NotifyIcon` (left-click toggles widget visibility, context menu exits the app). `MetricStatusToBrushConverter` maps `MetricStatus` → `Brush` for the Normal/Warning/Critical coloring used by both windows.
- `App.xaml.cs` is the composition root: builds the five providers → aggregator → view model → windows → tray icon, loads/saves `AppSettings` on startup/exit, and owns the `DispatcherTimer`. `App.xaml` sets `ShutdownMode="OnExplicitShutdown"` so hiding the widget or closing the popup doesn't quit the app — only the tray "종료" menu item calls `Shutdown()`.

## WPF + WinForms gotchas (already worked around — watch for regressions)

- `UseWPF` and `UseWindowsForms` are both enabled (WinForms is only needed for the tray `NotifyIcon`). Any type name that exists in both namespaces becomes ambiguous at compile time — hit so far: `Application` (in `App.xaml.cs`) and `Brushes` (in `MetricStatusToBrushConverter.cs`), both resolved with a `using X = System.Windows.X;` alias. If a new ambiguous type shows up (e.g. `Timer`, `MessageBox`, `Clipboard`), alias it the same way rather than fully-qualifying every call site.
- Implicit usings do **not** include `System.IO` in this project (confirmed by an actual compiler error, cause not fully understood — likely the WPF SDK's implicit-usings set differs from the plain console/library one). Add `using System.IO;` explicitly wherever `Path`/`File`/`Directory` are used; don't assume `ImplicitUsings` covers it.
- Never do metric collection synchronously on the UI thread. `GpuMetricsProvider` enumerates every `GPU Engine` performance counter instance (can be dozens on a busy system) and needs a warm-up delay between two samples; doing that inline in `DispatcherTimer.Tick` measurably froze the UI (drag stopped registering, updates stuttered) — this was a real bug, not just theoretical. The fix pattern is in `App.xaml.cs`: `await Task.Run(() => aggregator.CollectSnapshot())` before touching the view model.

## Testing

`tests/SysMonWidget.Tests` (xUnit) only covers logic that doesn't touch Windows-only APIs: `ThresholdEvaluator`, `NetworkUtilizationCalculator`, `MetricsAggregator` (via a fake `IMetricsProvider`), `WidgetViewModel`, `SettingsService`. The actual `PerformanceCounter`-based providers, XAML views, tray icon, and registry startup registration have no automated coverage — they're Windows/hardware-dependent and are verified by running the app.
