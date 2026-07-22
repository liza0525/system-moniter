# SysMonWidget UX 추가 개선: 위젯에 항상 보이는 메뉴 버튼 — 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

## Context

이전 라운드([2026-07-22-discoverability-ux.md](2026-07-22-discoverability-ux.md), 이미 구현·커밋됨)에서 트레이 풍선 도움말/툴팁/메뉴, 위젯 호버 툴팁, 팝업 투명도 슬라이더를 추가했다. 하지만 사용자는 이 방식이 여전히 별로라고 평가했다 — 힌트가 전부 **트레이(숨겨짐)** 또는 **호버(마우스를 올려야만 보임)**에 의존하기 때문. 사용자의 요구: "화면에 대놓고 메뉴처럼 보이는 게 가장 직관적" — 즉, 마우스를 올리지 않아도 항상 보이는 메뉴 형태의 UI 요소가 위젯 위에 직접 있어야 한다.

이번 작업의 목표: 위젯(`WidgetWindow`) 우측 상단에 항상 보이는 작은 메뉴 버튼("⋮")을 추가하고, 클릭하면 "설정 열기 / 위젯 숨기기 / 종료" 메뉴가 버튼 바로 아래에 드롭다운으로 뜨게 한다. 기존 동작(드래그 이동, 위젯 아무 곳이나 우클릭 시 팝업, 트레이 아이콘 좌클릭 토글/우클릭 메뉴, 호버 툴팁)은 전부 그대로 유지한다.

## 설계 결정 및 근거 (Plan 서브에이전트로 WPF 동작 검증 완료)

- **`Border`의 자식을 `StackPanel` 단독에서 `Grid`로 바꾸고, 기존 `StackPanel`과 새 `Button`을 같은 `Grid` 안에 겹쳐서 배치한다.** `Border`는 자식을 하나만 가질 수 있고, `Grid`는 여러 자식을 기본적으로 겹쳐 그리므로 오버레이 배치에 적합하다. 기존 5개 `TextBlock`(바인딩/컨버터 포함)은 전혀 손대지 않는다 — `StackPanel`에 `Margin="0,0,16,0"`만 추가해 가장 긴 라벨("DISK 100%")이 버튼 밑에 가리지 않게 한다.
- **버튼 클릭이 window의 `MouseLeftButtonDown`(드래그)로 버블링되지 않는다.** `ButtonBase.OnMouseLeftButtonDown`이 라우팅된 이벤트를 내부적으로 `Handled = true` 처리하기 때문에, 버튼 위를 클릭해도 `Window_MouseLeftButtonDown`이 실행되지 않는다. 별도 `e.Handled` 처리를 추가할 필요 없음.
- **버튼 우클릭도 `Window_MouseRightButtonUp`(팝업 토글)로 버블링되지 않는다.** 버튼에 `ContextMenu`가 지정되어 있으면 WPF 내부 처리기가 우클릭을 가로채 컨텍스트 메뉴를 열고 이벤트를 `Handled` 처리한다. 다만 이 부분은 이 환경에서 실제 컴파일/실행 검증이 불가능했으므로(WSL) 수동 검증 체크리스트에 별도 항목으로 남긴다 — 만약 실제로 팝업이 같이 열리는 문제가 발견되면, `Window_MouseRightButtonUp`에서 `e.OriginalSource`를 확인해 버튼 클릭이면 무시하도록 방어 코드를 추가한다.
- **메뉴 위치는 `PlacementTarget`/`Placement=Bottom`을 코드비하인드에서 명시적으로 지정한다.** 단순히 `ContextMenu.IsOpen = true`만 호출하면 기본 `Placement=MousePoint`라 클릭한 정확한 픽셀 위치에 따라 메뉴 위치가 흔들린다. "버튼에 붙은 메뉴"처럼 항상 동일한 위치(버튼 바로 아래)에 뜨게 하려면 `PlacementTarget = button; Placement = PlacementMode.Bottom;`을 `Click` 핸들러에서 먼저 설정해야 한다.
- **새 이벤트를 만들지 않는다.** "설정 열기" 메뉴 항목은 `WidgetWindow`에 이미 있는 `TogglePopupRequested` 이벤트를 그대로 재사용한다 (기존 우클릭 핸들러와 동일한 이벤트를 발생시킴). `App.xaml.cs`는 이미 `_widgetWindow.TogglePopupRequested += OnTogglePopupRequested;`로 구독 중이므로 **`App.xaml.cs`/`TrayIconManager.cs`는 이번 작업에서 전혀 수정하지 않는다.**
- **아이콘은 외부 리소스 없이 순수 유니코드 글리프("⋮", U+22EE)를 텍스트로 렌더링한다.** 이 프로젝트엔 이미지/아이콘 폰트 리소스가 전혀 없으므로 불필요한 의존성을 추가하지 않는다.
- **"위젯 숨기기"는 `Visibility = Visibility.Hidden`으로 직접 처리한다.** `TrayIconManager`의 좌클릭 토글과 동일한 효과. 숨긴 뒤 다시 보이게 하는 유일한 경로는 여전히 트레이 아이콘 좌클릭이며, 이는 이번 작업에서 변경하지 않는다.
- **"종료"는 `Application.Current.Shutdown()`을 그대로 호출한다.** `TrayIconManager`의 "종료" 메뉴 항목과 동일한 호출이라 `App.OnExit`의 위치/투명도 저장 로직이 동일하게 적용된다. `WidgetWindow.xaml.cs`는 `System.Windows.Forms`를 import하지 않으므로 `Application`이 `System.Windows.Application`으로 모호함 없이 resolve된다 — 별도 alias 불필요.

## Global Constraints

- Windows 전용(WPF, `net8.0-windows`). WSL에서 개발하고 `/mnt/c/Users/new25/D/pjt/system-monitor`(동일 git remote `liza0525/system-moniter`)로 수정 파일을 복사해 `"/mnt/c/Program Files/dotnet/dotnet.exe"`로 빌드/실행 검증한다 (discoverability-ux 라운드에서 이미 사용한 방식).
- 빌드 전 `SysMonWidget.exe`가 실행 중이면 자기 바이너리를 잠그므로 `taskkill.exe`로 종료 후 재빌드한다.
- XAML 뷰는 자동 단위테스트 대상이 아니다 (CLAUDE.md 기존 방침) — 수동 실행 검증으로 대체.
- 기존 동작(드래그 이동, 우클릭→팝업, 트레이 좌클릭 토글/우클릭 메뉴, 호버 툴팁, 임계값 저장, 투명도 슬라이더)은 회귀 없이 그대로 유지되어야 한다.
- **커밋은 사용자가 명시적으로 지시할 때만 진행한다** (CLAUDE.md 정책).
- **구현은 이 계획이 승인된 직후 자동으로 시작하지 않는다.** 사용자가 별도로 "시작해"라고 지시할 때까지 대기한다.

---

## 우선순위 개요

| 우선순위 | 의미 | 포함 Task |
|---|---|---|
| **P0** | 이번 라운드의 목적 자체 — 없으면 요청이 해결되지 않는 핵심 | 1 |
| **P1** | 조건부 후속 조치 — Task 1의 수동 검증 결과에 따라 필요 여부가 갈림 | 2 |

**근거:**
- Task 1(메뉴 버튼 자체 추가)이 이번 요청의 전부다 — "화면에 항상 보이는 메뉴"라는 요구를 직접 충족시키는 유일한 변경이라 P0.
- Task 2(우클릭 충돌 방어 코드)는 설계 근거에서 이미 "버튼에 `ContextMenu`가 있으면 WPF가 우클릭을 알아서 가로챈다"고 판단했지만, 이 환경(WSL)에서는 실제로 컴파일·실행해 검증할 수 없었던 가정이다. Task 1의 Step 4-6 수동 검증에서 문제가 없으면 Task 2는 아예 필요 없고, 문제가 발견될 때만 적용하는 조건부 작업이라 P1으로 분리한다.

---

## Task 1: 위젯 메뉴 버튼 추가

**Priority:** P0

**Files:**
- Modify: `src/SysMonWidget/Views/WidgetWindow.xaml`
- Modify: `src/SysMonWidget/Views/WidgetWindow.xaml.cs`

**Interfaces:**
- Consumes: 기존 `TogglePopupRequested` 이벤트(재사용, 신규 아님).
- Produces: 없음 (외부에서 참조할 새 public 멤버 없음 — `App.xaml.cs`/`TrayIconManager.cs` 변경 불필요).

- [ ] **Step 1: `WidgetWindow.xaml`의 `Border` 내부를 `Grid` 기반으로 교체**

`Border` 안의 기존 `<StackPanel>...</StackPanel>` 전체를 다음으로 교체:

```xml
        <Grid>
            <StackPanel Margin="0,0,16,0">
                <TextBlock Text="{Binding CpuUsagePercent, StringFormat='CPU {0:F0}%'}"
                           Foreground="{Binding CpuStatus, Converter={StaticResource StatusToBrush}}" />
                <TextBlock Text="{Binding MemoryUsagePercent, StringFormat='RAM {0:F0}%'}"
                           Foreground="{Binding MemoryStatus, Converter={StaticResource StatusToBrush}}" />
                <TextBlock Text="{Binding GpuUsagePercent, StringFormat='GPU {0:F0}%'}"
                           Foreground="{Binding GpuStatus, Converter={StaticResource StatusToBrush}}" />
                <TextBlock Text="{Binding DiskUsagePercent, StringFormat='DISK {0:F0}%'}"
                           Foreground="{Binding DiskStatus, Converter={StaticResource StatusToBrush}}" />
                <TextBlock Text="{Binding NetworkUsagePercent, StringFormat='NET {0:F0}%'}"
                           Foreground="{Binding NetworkStatus, Converter={StaticResource StatusToBrush}}" />
            </StackPanel>

            <Button x:Name="MenuButton"
                    Content="⋮"
                    Width="20" Height="20"
                    Padding="0"
                    HorizontalAlignment="Right"
                    VerticalAlignment="Top"
                    Background="Transparent"
                    BorderThickness="0"
                    Foreground="#CCCCCC"
                    FontSize="16"
                    FontWeight="Bold"
                    Cursor="Hand"
                    Focusable="False"
                    ToolTip="메뉴"
                    Click="MenuButton_Click">
                <Button.ContextMenu>
                    <ContextMenu>
                        <MenuItem Header="설정 열기" Click="OpenSettingsMenuItem_Click" />
                        <MenuItem Header="위젯 숨기기" Click="HideWidgetMenuItem_Click" />
                        <Separator />
                        <MenuItem Header="종료" Click="ExitMenuItem_Click" />
                    </ContextMenu>
                </Button.ContextMenu>
            </Button>
        </Grid>
```

(`Border`의 `ToolTip`/`ToolTipService.InitialShowDelay` 속성은 그대로 둔다 — 위젯 본체 호버 툴팁은 유지.)

- [ ] **Step 2: `WidgetWindow.xaml.cs`에 using 및 클릭 핸들러 4개 추가**

파일 상단 `using` 목록:

```csharp
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using SysMonWidget.Models;
using SysMonWidget.ViewModels;
```

`Window_MouseRightButtonUp` 메서드 뒤에 추가:

```csharp
    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        if (button.ContextMenu is null)
        {
            return;
        }

        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.Placement = PlacementMode.Bottom;
        button.ContextMenu.IsOpen = true;
    }

    private void OpenSettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        TogglePopupRequested?.Invoke(this, EventArgs.Empty);
    }

    private void HideWidgetMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Visibility = Visibility.Hidden;
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
```

- [ ] **Step 3: Windows 쪽 복사 + 빌드 확인**

```bash
cp src/SysMonWidget/Views/WidgetWindow.xaml /mnt/c/Users/new25/D/pjt/system-monitor/src/SysMonWidget/Views/WidgetWindow.xaml
cp src/SysMonWidget/Views/WidgetWindow.xaml.cs /mnt/c/Users/new25/D/pjt/system-monitor/src/SysMonWidget/Views/WidgetWindow.xaml.cs
```

실행 중인 `SysMonWidget.exe`가 있으면 먼저 종료 (`/mnt/c/Windows/System32/taskkill.exe /IM SysMonWidget.exe /F`), 이후:

```bash
cd /mnt/c/Users/new25/D/pjt/system-monitor && "/mnt/c/Program Files/dotnet/dotnet.exe" build src/SysMonWidget
```

Expected: 빌드 성공, 오류 0개.

- [ ] **Step 4: 수동 검증 (Windows에서 앱 실행)**

1. 실행 직후 위젯 우측 상단에 "⋮" 글리프가 호버 없이 항상 보이는지 확인.
2. 위젯 본체(버튼 제외)를 드래그 → 기존과 동일하게 창이 이동하는지 확인.
3. "⋮" 버튼 위에서 드래그 시도 → 창이 드래그되지 않고 대신 메뉴가 열리는지 확인 (설계 근거 (a) 검증).
4. 위젯 본체(버튼 제외)를 우클릭 → 기존과 동일하게 팝업이 열리는지 확인 (회귀 없음).
5. "⋮" 버튼 좌클릭 → 버튼 바로 아래에 "설정 열기 / 위젯 숨기기 / 종료" 메뉴가 항상 같은 위치에 뜨는지 확인 (설계 근거 placement 검증).
6. "⋮" 버튼 우클릭 → 팝업이 동시에 뜨지 않고 메뉴만 열리는지 확인 (설계 근거 (c) 검증 — **가장 불확실한 부분이라 특히 꼼꼼히 확인**). 만약 팝업이 같이 열리면 아래 Task 2(P1)를 이어서 적용한다.
7. 메뉴에서 "설정 열기" 클릭 → 팝업이 열리는지 확인.
8. 메뉴에서 "위젯 숨기기" 클릭 → 위젯이 사라지고, 트레이 아이콘 좌클릭으로 다시 보이는지 확인.
9. 메뉴에서 "종료" 클릭 → 트레이 아이콘까지 포함해 앱이 정상 종료되는지 확인.
10. 위젯 본체(버튼 제외) 호버 → 기존 툴팁("드래그하여 위치 이동 · 우클릭으로 상세 설정 열기")이 그대로 뜨는지 확인.
11. `SizeToContent="WidthAndHeight"`이므로 버튼이 창 가장자리에서 잘리지 않고, 창 크기가 부자연스럽게 커지지 않았는지 확인.

- [ ] **Step 5: 커밋 (사용자 승인 후)**

```bash
git add src/SysMonWidget/Views/WidgetWindow.xaml src/SysMonWidget/Views/WidgetWindow.xaml.cs
git commit -m "feat(widget): add always-visible menu button for settings/hide/exit"
```

---

## Task 2 (조건부): 우클릭 충돌 방어 코드

**Priority:** P1 — Task 1의 Step 4-6 수동 검증에서 실제로 문제가 확인될 때만 진행. 문제가 없으면 이 Task는 통째로 생략한다.

**Files:**
- Modify: `src/SysMonWidget/Views/WidgetWindow.xaml.cs`

**적용 조건:** "⋮" 버튼을 우클릭했을 때 버튼의 `ContextMenu`뿐 아니라 `Window_MouseRightButtonUp`도 같이 실행되어 팝업이 동시에 뜨는 경우.

- [ ] **Step 1: `Window_MouseRightButtonUp`에 방어 코드 추가**

```csharp
    private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && FindAncestor<Button>(source) is not null)
        {
            return;
        }

        TogglePopupRequested?.Invoke(this, EventArgs.Empty);
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
```

(`using System.Windows.Media;`가 `VisualTreeHelper` 사용을 위해 추가로 필요하다.)

- [ ] **Step 2: Windows 쪽 복사 + 빌드 + 재검증**

Task 1의 Step 3-4와 동일한 방식으로 복사·빌드 후, "⋮" 버튼 우클릭 시 팝업이 더 이상 같이 뜨지 않는지만 다시 확인한다.

- [ ] **Step 3: 커밋 (사용자 승인 후)**

```bash
git add src/SysMonWidget/Views/WidgetWindow.xaml.cs
git commit -m "fix(widget): prevent popup from opening when right-clicking the menu button"
```
