# SysMonWidget UX 개선: 기능 발견성(Discoverability) + 투명도 슬라이더 — 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 위젯/트레이/팝업에 숨겨져 있던 조작법(트레이 좌클릭 토글, 위젯 우클릭 팝업, 트레이 메뉴, 팝업 설정)을 힌트로 드러내고, PRD가 요구하지만 실제로는 없던 투명도 조절 UI를 팝업에 추가한다.

## Context

사용자가 위젯/팝업 UX 개선을 요청했고, 대화를 통해 구체적인 문제를 좁혔다: **화면에 숫자만 떠 있어서, README를 읽지 않는 한 부수 기능의 존재 자체를 알 수 없다.** 구체적으로 발견되지 않는 기능 4가지:

1. 트레이 아이콘 좌클릭 → 위젯 표시/숨김 토글
2. 위젯 우클릭 → 팝업(상세 설정) 토글
3. 팝업 안의 임계값 편집 / 자동실행 설정 (애초에 팝업을 여는 법을 몰라서 도달 불가)
4. 트레이 아이콘 우클릭 메뉴 (현재 "종료" 하나뿐)

추가로 코드를 살펴보던 중 발견한 것: PRD(`PRD.md` D항)는 "위젯 투명도 사용자 조절 가능"을 요구하지만, 실제 코드에는 투명도를 바꿀 수 있는 UI가 전혀 없다 (`AppSettings.Opacity` 기본값 0.85가 그냥 고정되어 있고, 종료 시 현재 값을 저장만 함). 이번 발견성 개선과 맞물리는 부분이라 같이 처리한다 — 팝업을 찾아야 설정에 도달할 수 있다는 점이 동일한 문제이기 때문.

**Architecture:** 기존 동작(드래그 이동, 우클릭 팝업, 트레이 좌클릭 토글, 임계값 저장)은 변경하지 않는다. `SettingsService`에 첫 실행 감지용 메서드를 추가하고, `TrayIconManager`에 툴팁/풍선 도움말/새 메뉴 항목을, `WidgetWindow.xaml`에 호버 툴팁을, `PopupWindow`에 투명도 슬라이더(라이브 미리보기, `App.xaml.cs`를 통해 `WidgetWindow.Opacity`에 직접 반영)를 추가한다. 온보딩 마법사나 영구 "다시 보지 않기" 플래그 같은 과한 장치는 넣지 않는다.

**Tech Stack:** C#/.NET 8 WPF, `System.Windows.Forms.NotifyIcon`(트레이), `System.Windows.Controls.Slider`, xUnit.

## 설계 결정 및 근거

- **첫 실행 감지는 별도 플래그 없이 `settings.json` 존재 여부로 판단한다.** `SettingsService`에 `public bool SettingsFileExists() => File.Exists(_filePath);`를 추가하고, `App.OnStartup`에서 `Load()` 호출 전에 확인한다. `AppSettings`에 `HasSeenHints` 같은 필드를 추가하는 것보다 단순함 — 스키마 변경도, 힌트를 보여준 뒤 별도로 `Save()`를 호출할 필요도 없다. `settings.json`을 지우면 풍선 도움말이 다시 뜨는 게 유일한 차이인데, 이는 위치/임계값이 "초기화"되는 것과 동일한 자연스러운 동작이라 문제 없음.
- **투명도는 `WidgetViewModel`을 거치지 않고 `PopupWindow` → `App` → `WidgetWindow`로 직접 연결한다.** `WidgetViewModel`은 지표/임계값 전용 `INotifyPropertyChanged` 표면이고, `Opacity`는 `Window`의 속성이라 뷰모델을 거치게 하면 불필요한 책임 확장이 된다. `App.xaml.cs`는 이미 `_widgetWindow` 필드를 들고 있고 `OnExit`에서 `_widgetWindow.Opacity`를 읽어 저장하는 로직이 이미 있으므로, 슬라이더가 라이브로 `_widgetWindow.Opacity`를 바꾸기만 하면 기존 저장 경로가 그대로 작동한다.
- **트레이 메뉴의 "설정 열기"는 기존 `TogglePopupRequested`/`OnTogglePopupRequested` 경로를 재사용한다.** 팝업을 생성하는 코드 경로를 하나로 유지하기 위함.
- **Task 순서는 각 Task가 독립적으로 빌드되도록 구성한다.** `TrayIconManager`/`PopupWindow`의 생성자 시그니처가 바뀌므로, `App.xaml.cs`에서 해당 호출부만 같은 Task 안에 묶어 중간 상태에서도 항상 컴파일되게 한다.

## Global Constraints

- 이 저장소는 Windows 전용(WPF, `net8.0-windows`)이다. WSL에서 빌드하려면 CLAUDE.md에 따라 Windows `dotnet.exe`를 직접 호출한다: `"/mnt/c/Program Files/dotnet/dotnet.exe" build`.
- `--project` 경로는 항상 슬래시(`/`)를 사용한다 (백슬래시+대문자는 bash류 셸에서 이스케이프로 소실됨).
- `NotifyIcon`, XAML 뷰, 트레이 아이콘 등 Windows/GUI 전용 코드는 자동 단위테스트 대상이 아니다 (CLAUDE.md 기존 방침과 동일) — 수동 실행 검증으로 대체한다.
- 기존 동작(드래그 이동, 우클릭→팝업, 좌클릭→표시/숨김, 임계값 저장, 자동실행 체크박스)은 회귀 없이 그대로 유지되어야 한다.
- **커밋은 각 Task 끝에 명령어가 적혀 있어도 사용자 승인 없이 자동 실행하지 않는다** (CLAUDE.md `절대 자동으로 git commit을 실행하지 말 것` 정책). 각 Task 완료 후 변경 파일 요약만 보여주고 커밋 여부는 사용자 판단에 맡긴다.
- 커밋 메시지는 `type(scope): description` 컨벤션을 따른다.
- **구현은 사용자가 명시적으로 시작하라고 지시할 때만 진행한다.** 계획 승인과 구현 시작 지시는 별개다 — 계획을 세우거나 문서화하는 것이 곧 코드를 수정해도 된다는 뜻은 아니다.

---

## 우선순위 개요

| 우선순위 | 의미 | 포함 Task |
|---|---|---|
| **P0** | 없으면 발견성 문제 자체가 해결되지 않는 핵심 경로 | 1, 2 |
| **P1** | 발견성을 보강하는 추가 힌트 (P0만으로도 설정에 도달은 가능) | 3 |
| **P2** | PRD 상 원래 있어야 했지만 누락돼 있던 기능 보완 — 발견성과는 별개 문제 | 4 |

**근거:**
- Task 1(`SettingsFileExists`)은 Task 2가 의존하는 전제조건이라 P0.
- Task 2(트레이 풍선 도움말 + "설정 열기" 메뉴)는 사용자가 우클릭 제스처를 전혀 몰라도 트레이 메뉴만으로 팝업(임계값/자동실행 설정)에 도달할 수 있게 하는 **유일한 안전망**이라 P0 — 이게 없으면 원래 문제("팝업 안 기능을 못 찾음")가 그대로 남는다.
- Task 3(위젯 호버 툴팁)은 우클릭 제스처를 사용자가 직접 발견하도록 돕는 보강 힌트다. Task 2가 이미 대체 경로(트레이 메뉴)를 제공하므로, 이게 없어도 기능 자체엔 도달 가능 — 그래서 P1.
- Task 4(투명도 슬라이더)는 발견성 문제가 아니라 애초에 PRD에 있었어야 할 UI가 통째로 빠져 있던 별개의 기능 누락이다. 이번 작업 범위에 포함은 시키되, 발견성 핵심 경로(P0/P1)보다는 우선순위가 낮은 P2로 둔다.

---

## Task 1: `SettingsService.SettingsFileExists()` — 첫 실행 감지용 메서드

**Priority:** P0 (이후 Task 2가 이 메서드에 의존)

**Files:**
- Modify: `src/SysMonWidget/Services/SettingsService.cs`
- Test: `tests/SysMonWidget.Tests/SettingsServiceTests.cs`

**Interfaces:**
- Produces: `public bool SettingsFileExists()` — `settings.json`이 아직 없으면(=첫 실행) `false`.

- [ ] **Step 1: 실패하는 테스트 작성**

`tests/SysMonWidget.Tests/SettingsServiceTests.cs`의 `SaveThenLoad_RoundTripsValues` 테스트 뒤, `Dispose()` 앞에 추가:

```csharp
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
```

- [ ] **Step 2: 테스트 실패 확인**

Run: `dotnet test --filter "FullyQualifiedName~SettingsServiceTests"`
Expected: 컴파일 실패 — `SettingsFileExists` 메서드가 아직 없음.

- [ ] **Step 3: 최소 구현**

`src/SysMonWidget/Services/SettingsService.cs`에서 `public AppSettings Load()` 메서드 바로 위에 추가:

```csharp
    public bool SettingsFileExists() => File.Exists(_filePath);

```

- [ ] **Step 4: 테스트 통과 확인**

Run: `dotnet test --filter "FullyQualifiedName~SettingsServiceTests"`
Expected: PASS — 기존 2개 + 신규 2개, 총 4개 테스트 통과.

- [ ] **Step 5: 커밋 (사용자 승인 후)**

```bash
git add src/SysMonWidget/Services/SettingsService.cs tests/SysMonWidget.Tests/SettingsServiceTests.cs
git commit -m "feat(settings): add SettingsFileExists for first-launch detection"
```

---

## Task 2: 트레이 아이콘 발견성 개선 (툴팁/풍선 도움말/설정 메뉴)

**Priority:** P0

**Files:**
- Modify: `src/SysMonWidget/Views/TrayIconManager.cs`
- Modify: `src/SysMonWidget/App.xaml.cs` (`OnStartup`의 트레이 관련 부분만)

**Interfaces:**
- Consumes: Task 1의 `SettingsService.SettingsFileExists()`.
- Produces: `TrayIconManager(Window widgetWindow, bool isFirstLaunch)` 생성자, `event EventHandler? OpenSettingsRequested`.

> Windows 전용 `NotifyIcon` API라 자동 단위테스트 대상에서 제외하고 수동 검증으로 대체한다 (CLAUDE.md 테스트 방침과 동일 패턴).

- [ ] **Step 1: `TrayIconManager.cs` 전체 교체**

```csharp
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace SysMonWidget.Views;

public class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Window _widgetWindow;

    public event EventHandler? OpenSettingsRequested;

    public TrayIconManager(Window widgetWindow, bool isFirstLaunch)
    {
        _widgetWindow = widgetWindow;

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("설정 열기", null, (_, _) => OpenSettingsRequested?.Invoke(this, EventArgs.Empty));
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("종료", null, (_, _) => Application.Current.Shutdown());

        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "SysMonWidget (좌클릭: 표시/숨김, 우클릭: 메뉴)",
            ContextMenuStrip = contextMenu,
        };

        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                _widgetWindow.Visibility = _widgetWindow.IsVisible ? Visibility.Hidden : Visibility.Visible;
            }
        };

        if (isFirstLaunch)
        {
            _notifyIcon.BalloonTipTitle = "SysMonWidget 사용법";
            _notifyIcon.BalloonTipText = "좌클릭: 위젯 표시/숨김\n우클릭(위젯): 상세 설정 열기";
            _notifyIcon.ShowBalloonTip(5000);
        }
    }

    public void Dispose() => _notifyIcon.Dispose();
}
```

- [ ] **Step 2: `App.xaml.cs`의 `OnStartup` 수정**

`_appSettings = _settingsService.Load();` 줄을 다음으로 교체 (첫 실행 여부를 `Load()` 호출 전에 확인):

```csharp
        _settingsService = new SettingsService();
        var isFirstLaunch = !_settingsService.SettingsFileExists();
        _appSettings = _settingsService.Load();
```

`_trayIconManager = new TrayIconManager(_widgetWindow);` 줄을 다음으로 교체:

```csharp
        _trayIconManager = new TrayIconManager(_widgetWindow, isFirstLaunch);
        _trayIconManager.OpenSettingsRequested += OnTogglePopupRequested;
```

- [ ] **Step 3: 빌드 확인**

Run (WSL에서 Windows SDK 사용 시): `"/mnt/c/Program Files/dotnet/dotnet.exe" build --project src/SysMonWidget`
Expected: 빌드 성공.

- [ ] **Step 4: 수동 검증 (Windows에서 앱 실행)**

1. `%AppData%\SysMonWidget\settings.json` 삭제(또는 이름 변경) 후 `dotnet run --project src/SysMonWidget` 실행 → 풍선 도움말이 1회 노출되는지 확인.
2. 앱을 종료 후 재실행 → 풍선 도움말이 다시 뜨지 않는지 확인.
3. 트레이 아이콘에 마우스를 올려 툴팁 텍스트("좌클릭: 표시/숨김, 우클릭: 메뉴")가 보이는지 확인.
4. 트레이 아이콘 우클릭 → "설정 열기"가 "종료" 위에 보이고, 클릭 시 팝업이 열리는지 확인.
5. 트레이 아이콘 좌클릭 → 기존과 동일하게 위젯 표시/숨김이 토글되는지 확인 (회귀 없음).

- [ ] **Step 5: 커밋 (사용자 승인 후)**

```bash
git add src/SysMonWidget/Views/TrayIconManager.cs src/SysMonWidget/App.xaml.cs
git commit -m "feat(tray): add first-launch balloon tip, tooltip, and settings menu item"
```

---

## Task 3: 위젯 호버 툴팁

**Priority:** P1 (Task 2와 독립적, 순서 무관)

**Files:**
- Modify: `src/SysMonWidget/Views/WidgetWindow.xaml`

**Interfaces:**
- Consumes: 없음 (순수 XAML 추가, 코드비하인드 변경 없음).

- [ ] **Step 1: `Border`에 툴팁 추가**

`src/SysMonWidget/Views/WidgetWindow.xaml`에서:

```xml
    <Border Padding="16" CornerRadius="8" Background="#1E1E1E" MinWidth="150">
```

를 다음으로 교체:

```xml
    <Border Padding="16" CornerRadius="8" Background="#1E1E1E" MinWidth="150"
            ToolTip="드래그하여 위치 이동 · 우클릭으로 상세 설정 열기"
            ToolTipService.InitialShowDelay="500">
```

- [ ] **Step 2: 빌드 확인**

Run: `"/mnt/c/Program Files/dotnet/dotnet.exe" build --project src/SysMonWidget`
Expected: 빌드 성공.

- [ ] **Step 3: 수동 검증**

위젯 위에 마우스를 올린 채 약 0.5초 대기 → 툴팁이 나타나는지 확인. 클릭 없이 호버만 했을 때 드래그/팝업 동작에 영향이 없는지 확인.

- [ ] **Step 4: 커밋 (사용자 승인 후)**

```bash
git add src/SysMonWidget/Views/WidgetWindow.xaml
git commit -m "feat(widget): add hover tooltip explaining drag and right-click"
```

---

## Task 4: 팝업 투명도 슬라이더

**Priority:** P2

**Files:**
- Modify: `src/SysMonWidget/Views/PopupWindow.xaml`
- Modify: `src/SysMonWidget/Views/PopupWindow.xaml.cs`
- Modify: `src/SysMonWidget/App.xaml.cs` (`OnTogglePopupRequested`만)

**Interfaces:**
- Produces: `PopupWindow(WidgetViewModel viewModel, ThresholdSettings thresholds, double currentOpacity)` 생성자(매개변수 1개 추가), `event EventHandler<double>? OpacityChanged`.

- [ ] **Step 1: `PopupWindow.xaml`에 슬라이더 추가**

`RunAtStartupCheckBox` 줄과 `SaveButton` 줄 사이에 삽입:

```xml
        <CheckBox x:Name="RunAtStartupCheckBox" Content="Windows 시작 시 자동 실행" Margin="0,10,0,0" />

        <Separator Margin="0,10" />
        <TextBlock Text="위젯 투명도" FontWeight="Bold" Margin="0,0,0,6" />
        <Slider x:Name="OpacitySlider" Minimum="0.3" Maximum="1.0"
                TickFrequency="0.1" ValueChanged="OpacitySlider_ValueChanged" />

        <Button x:Name="SaveButton" Content="저장" Margin="0,10,0,0" Click="SaveButton_Click" />
```

- [ ] **Step 2: `PopupWindow.xaml.cs` 수정**

생성자 시그니처와 본문, 새 이벤트를 추가:

```csharp
    public event EventHandler<ThresholdSettings>? ThresholdsSaved;
    public event EventHandler<double>? OpacityChanged;

    public PopupWindow(WidgetViewModel viewModel, ThresholdSettings thresholds, double currentOpacity)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _thresholds = thresholds;

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
```

(참고: `OpacitySlider.Value = currentOpacity;`가 생성자 안에서 `ValueChanged`를 즉시 한 번 발생시키지만, 이 시점엔 `App`이 아직 `OpacityChanged`를 구독하기 전이라 아무 효과가 없다 — 안전한 no-op.)

- [ ] **Step 3: `App.xaml.cs`의 `OnTogglePopupRequested` 수정**

```csharp
    private void OnTogglePopupRequested(object? sender, EventArgs e)
    {
        var popup = new PopupWindow(_viewModel!, _appSettings!.Thresholds, _widgetWindow!.Opacity);
        popup.ThresholdsSaved += (_, updatedThresholds) =>
        {
            _appSettings.Thresholds = updatedThresholds;
            _settingsService!.Save(_appSettings);
        };
        popup.OpacityChanged += (_, opacity) => _widgetWindow!.Opacity = opacity;
        popup.Show();
    }
```

(`OnExit`은 이미 `_widgetWindow.Opacity`를 읽어 저장하므로 추가 변경 불필요 — 슬라이더로 바뀐 값이 다음 실행 시 그대로 복원된다.)

- [ ] **Step 4: 빌드 확인**

Run: `"/mnt/c/Program Files/dotnet/dotnet.exe" build --project src/SysMonWidget`
Expected: 빌드 성공.

- [ ] **Step 5: 수동 검증**

1. 팝업을 열고 투명도 슬라이더를 드래그 → 위젯 배경 투명도가 실시간으로 바뀌는지 확인 (드래그 중, 놓기 전에도).
2. 슬라이더를 원하는 값에 둔 채 트레이 메뉴 "종료"로 앱 종료 후 재실행 → 마지막 투명도가 복원되는지 확인.
3. 임계값 텍스트박스 수정 후 "저장" 클릭 → 기존 임계값 저장/자동실행 체크박스 동작에 영향이 없는지 확인 (회귀 없음).

- [ ] **Step 6: 커밋 (사용자 승인 후)**

```bash
git add src/SysMonWidget/Views/PopupWindow.xaml src/SysMonWidget/Views/PopupWindow.xaml.cs src/SysMonWidget/App.xaml.cs
git commit -m "feat(popup): add live opacity slider"
```

---

## 전체 회귀 검증 (Task 1~4 완료 후)

Run: `dotnet test`
Expected: `ThresholdEvaluator`/`NetworkUtilizationCalculator`/`MetricsAggregator`/`WidgetViewModel`/`SettingsService` 전체 스위트 통과 (기존 테스트 + Task 1에서 추가한 2개).

Windows에서 `dotnet run --project src/SysMonWidget`으로 전체 시나리오(첫 실행 풍선 도움말 → 트레이 툴팁/메뉴 → 위젯 툴팁 → 팝업 투명도 슬라이더 → 임계값 저장 → 재실행 후 상태 복원)를 순서대로 한 번 더 통짜로 확인한다.
