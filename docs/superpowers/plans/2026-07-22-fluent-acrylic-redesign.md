# SysMonWidget Fluent/Acrylic 리디자인 — 위젯·팝업 스타일 적용

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

## Context

이전 라운드([2026-07-22-discoverability-ux.md](2026-07-22-discoverability-ux.md), [2026-07-22-widget-menu-button.md](2026-07-22-widget-menu-button.md), [2026-07-22-popup-save-confirmation.md](2026-07-22-popup-save-confirmation.md), 모두 구현·커밋됨)에서 발견성과 기능은 갖췄지만, 위젯([WidgetWindow.xaml](../../../src/SysMonWidget/Views/WidgetWindow.xaml))은 여전히 `#1E1E1E` 단색 배경에 스타일링 없는 텍스트 나열이고, 팝업([PopupWindow.xaml](../../../src/SysMonWidget/Views/PopupWindow.xaml))은 기본 WPF 컨트롤이 그대로 노출된 상태다.

사용자가 세 가지 방향(Fluent/Acrylic, Rainmeter 미니멀 다크, 글래스모피즘/네온 HUD)을 실제 목업으로 비교해보고 **Fluent/Acrylic**을 선택했다. 같은 대화에서 나온 "케밥(⋮) 메뉴 버튼이 카드 위에 절대좌표로 얹혀 있어서 텍스트와 붙어 보인다"는 지적도 이번 작업에 함께 포함한다 — 버튼을 별도 헤더 행으로 분리하는 걸로 자연스럽게 해결된다.

**구현 전 기술 조사에서 확인한 제약:** WPF에서 진짜 OS 블러(배경이 실제로 흐려 보이는 Acrylic)를 내려면 비공식 Win32 API(`SetWindowCompositionAttribute`)를 P/Invoke로 직접 부르거나 서드파티 NuGet이 필요하다. 그런데 이 API는 **Windows 10 1903부터 창을 드래그할 때 랙이 걸리는 알려진 버그**가 있고, 이 위젯의 핵심 조작이 바로 좌클릭 드래그 이동이라 정면으로 충돌한다. 사용자와 상의 후, **순수 WPF 브러시/그림자로 Fluent의 시각적 언어만 재현하는 "가짜 아크릴"**로 확정했다 — 새 NuGet 의존성 없음, P/Invoke 없음, 드래그 성능 회귀 위험 없음.

## 설계 결정 및 근거

- **팝업은 네이티브 창 틀(제목표시줄)을 그대로 유지한다.** 목업은 완전히 프레임 없는 떠 있는 카드였지만, 그대로 구현하려면 커스텀 닫기 버튼·드래그·Alt+F4 대체 동작을 새로 만들어야 해서 회귀 위험이 크다. Windows 11은 네이티브 창도 자동으로 모서리를 둥글게 그려주므로 시각적 차이는 크지 않다. 대신 창 **내부 콘텐츠 영역**을 카드처럼 스타일링한다(전체를 덮는 `Border`, `CornerRadius=0`로 창 모서리와의 불일치를 아예 만들지 않음 — 대신 내부 컨트롤들의 둥근 모서리로 Fluent 느낌을 냄).
- **위젯은 어두운 카드, 팝업은 사용자가 고른 대로 밝은 카드**로 서로 다른 표면을 쓴다. 그래서 상태색(정상/경고/위험)도 밝은/어두운 배경 각각에서 잘 보이는 값을 따로 두고, `MetricStatusToBrushConverter`가 `ConverterParameter`로 어느 표면인지 구분해서 반환한다.
- **색상 값의 단일 출처는 `Styles/FluentPalette.xaml` 하나뿐이다.** 컨버터도 하드코딩된 `Brushes.*` 대신 이 리소스 딕셔너리에서 키로 찾아 반환하도록 바꿔서, 나중에 색을 조정할 일이 생기면 한 곳만 고치면 되게 한다.
- **`TextBox`/`CheckBox`/`Slider`는 `ControlTemplate`을 새로 정의해서 재스킨한다.** WPF 기본 템플릿은 둥근 모서리·커스텀 색을 `Style` Setter만으로 낼 수 없는 컨트롤들이라 템플릿 교체가 필요하다 (`Button`/`TextBlock`은 Setter만으로 충분).

## Global Constraints

- Windows 전용(WPF, `net8.0-windows`). WSL에서 개발하고 이전 라운드와 동일한 방식으로 Windows 쪽 경로(`/mnt/c/Users/new25/D/pjt/system-monitor`, 동일 git remote)로 수정 파일을 복사해 `"/mnt/c/Program Files/dotnet/dotnet.exe"`로 빌드/실행 검증한다.
- 빌드 전 `SysMonWidget.exe`가 실행 중이면 자기 바이너리를 잠그므로 종료 후 재빌드한다.
- 새 NuGet 의존성 추가 없음, P/Invoke 없음 (기술 조사에 따른 확정 사항).
- `ThresholdEvaluator`/`SettingsService` 등 로직은 변경하지 않는다 — 이번 작업은 순수 시각 변경.
- XAML 뷰는 자동 단위테스트 대상이 아니다 (CLAUDE.md 기존 방침) — 수동 실행 검증으로 대체.
- 기존 동작(드래그 이동, 우클릭→팝업, 케밥 메뉴, 트레이 토글/메뉴, 호버 툴팁, 임계값 저장, 자동실행 체크박스, 투명도 슬라이더)은 회귀 없이 그대로 유지되어야 한다.
- 아래 XAML/C# 스니펫은 목표 구현이며, 이 환경(WSL)에서 실제 컴파일 검증은 불가능했다 — Windows 빌드에서 사소한 조정이 필요할 수 있다. 그런 조정은 계획서 수정 없이 커밋 본문에 이유만 남긴다.
- **커밋은 각 Task 끝에 사용자 승인 후에만 진행한다** (CLAUDE.md 정책).
- **구현은 이 계획이 승인된 직후 자동으로 시작하지 않는다.** 사용자가 별도로 "시작해"라고 지시할 때까지 대기한다.

---

## 우선순위 개요

| 우선순위 | 의미 | 포함 Task |
|---|---|---|
| **P0** | 이게 없으면 "리디자인이 적용됐다"고 말할 수 없는 핵심 표면 | 1, 2, 3 |
| **P1** | 설정 화면까지 동일한 비주얼 언어로 완성 | 4, 5 |
| **P2** | 있으면 좋지만 없어도 완료로 볼 수 있는 폴리시 | 6, 7 |

**근거:**
- Task 1(공유 팔레트 리소스)이 없으면 이후 모든 Task가 색을 하드코딩하게 되어 일관성이 깨진다 — P0.
- Task 2(`MetricStatusToBrushConverter` 개선)가 없으면 새 카드 배경 위에 기존 `Red/Gold/LightGreen`이 그대로 남아 색이 충돌한다 — P0.
- Task 3(위젯 카드 재구성, 케밥 헤더 포함)은 **항상 떠 있는 화면**이라 사용자가 리디자인을 체감하는 유일한 표면 — P0.
- Task 4(팝업 카드/컨트롤 재스타일)는 우클릭·트레이 메뉴를 거쳐야 도달하는 화면이라 위젯보다 노출 빈도가 낮다 — P1.
- Task 5(오퍼시티 슬라이더 커스텀 템플릿)는 `Slider` 기본 템플릿을 통째로 갈아엎어야 해서 실패 위험이 따로 분리할 만큼 크다 — 실패해도 Task 4의 나머지(입력창/체크박스/버튼)는 별개로 완결되도록 분리 — P1.
- Task 6(버튼 hover/press 피드백), Task 7(팝업 카드 안 작은 타이틀)은 없어도 리디자인 자체는 완결되는 순수 폴리시 — P2.

---

## Task 1: 공유 Fluent 팔레트 리소스 딕셔너리

**Priority:** P0

**Files:**
- New: `src/SysMonWidget/Styles/FluentPalette.xaml`
- Modify: `src/SysMonWidget/App.xaml`

**Interfaces:**
- Produces: `WidgetSurfaceBrush`, `WidgetBorderBrush`, `PopupSurfaceBrush`, `PopupBorderBrush`, `AccentBrush`, `TextPrimaryDarkBrush`, `TextSecondaryDarkBrush`, `TextPrimaryLightBrush`, `TextSecondaryLightBrush`, `StatusNormalDarkBrush`/`StatusWarningDarkBrush`/`StatusCriticalDarkBrush`, `StatusNormalLightBrush`/`StatusWarningLightBrush`/`StatusCriticalLightBrush` — Task 2~5가 전부 이 키들을 소비한다.

- [ ] **Step 1: `Styles/FluentPalette.xaml` 신규 작성**

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <LinearGradientBrush x:Key="WidgetSurfaceBrush" StartPoint="0,0" EndPoint="0,1">
        <GradientStop Color="#DE262B36" Offset="0" />
        <GradientStop Color="#DE1B1F27" Offset="1" />
    </LinearGradientBrush>
    <SolidColorBrush x:Key="WidgetBorderBrush" Color="#24FFFFFF" />

    <LinearGradientBrush x:Key="PopupSurfaceBrush" StartPoint="0,0" EndPoint="0,1">
        <GradientStop Color="#F2FFFFFF" Offset="0" />
        <GradientStop Color="#F2F5F7FA" Offset="1" />
    </LinearGradientBrush>
    <SolidColorBrush x:Key="PopupBorderBrush" Color="#14000000" />

    <SolidColorBrush x:Key="AccentBrush" Color="#3D8BFF" />

    <SolidColorBrush x:Key="TextPrimaryDarkBrush" Color="#F2F5F7" />
    <SolidColorBrush x:Key="TextSecondaryDarkBrush" Color="#9AA6B2" />
    <SolidColorBrush x:Key="TextPrimaryLightBrush" Color="#1A1D29" />
    <SolidColorBrush x:Key="TextSecondaryLightBrush" Color="#6B7280" />

    <SolidColorBrush x:Key="StatusNormalDarkBrush" Color="#6CCB5F" />
    <SolidColorBrush x:Key="StatusWarningDarkBrush" Color="#FFD23F" />
    <SolidColorBrush x:Key="StatusCriticalDarkBrush" Color="#FF6B6B" />

    <SolidColorBrush x:Key="StatusNormalLightBrush" Color="#1E8E3E" />
    <SolidColorBrush x:Key="StatusWarningLightBrush" Color="#C77700" />
    <SolidColorBrush x:Key="StatusCriticalLightBrush" Color="#C4314B" />

    <Style TargetType="TextBlock">
        <Setter Property="FontFamily" Value="Segoe UI Variable Text, Segoe UI" />
    </Style>

</ResourceDictionary>
```

- [ ] **Step 2: `App.xaml`에 병합**

`src/SysMonWidget/App.xaml`을 다음으로 교체:

```xml
<Application x:Class="SysMonWidget.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             ShutdownMode="OnExplicitShutdown">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Styles/FluentPalette.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

- [ ] **Step 3: 빌드 확인**

Windows 쪽 경로로 복사 후 `"/mnt/c/Program Files/dotnet/dotnet.exe" build src/SysMonWidget`. Expected: 빌드 성공(이 시점엔 아직 아무도 새 리소스를 참조하지 않으므로 시각적 변화 없음).

- [ ] **Step 4: 커밋 (사용자 승인 후)**

```bash
git add src/SysMonWidget/Styles/FluentPalette.xaml src/SysMonWidget/App.xaml
git commit -m "feat(styles): add shared Fluent palette resource dictionary"
```

---

## Task 2: `MetricStatusToBrushConverter`를 팔레트 기반으로 교체

**Priority:** P0

**Files:**
- Modify: `src/SysMonWidget/Converters/MetricStatusToBrushConverter.cs`

**Interfaces:**
- Consumes: Task 1의 `Status*Dark/LightBrush` 리소스.
- Produces: 기존과 동일한 `Convert` 시그니처, 다만 `ConverterParameter="Light"`를 받으면 밝은 배경용 브러시를 반환.

- [ ] **Step 1: 컨버터 전체 교체**

```csharp
using System.Globalization;
using System.Windows.Data;
using SysMonWidget.Models;
using Application = System.Windows.Application;

namespace SysMonWidget.Converters;

public class MetricStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var suffix = string.Equals(parameter as string, "Light", StringComparison.OrdinalIgnoreCase)
            ? "Light"
            : "Dark";

        var key = (MetricStatus)value switch
        {
            MetricStatus.Critical => $"StatusCritical{suffix}Brush",
            MetricStatus.Warning => $"StatusWarning{suffix}Brush",
            _ => $"StatusNormal{suffix}Brush",
        };

        return Application.Current.Resources[key];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

(`Application`이 `System.Windows.Forms`와 이름이 겹쳐서 `App.xaml.cs`와 동일하게 alias가 필요함 — CLAUDE.md의 기존 WPF+WinForms 모호성 gotcha 참고.)

- [ ] **Step 2: 빌드 확인**

Expected: 빌드 성공. 위젯을 아직 안 건드렸으므로 실행하면 기존과 같은 `Red/Gold/LightGreen` 대신 Task 1의 어두운 상태색(`#6CCB5F`/`#FFD23F`/`#FF6B6B`)으로 이미 바뀌어 보임 — 배경은 아직 `#1E1E1E`라 부자연스러울 수 있으나 Task 3에서 해결됨.

- [ ] **Step 3: 커밋 (사용자 승인 후)**

```bash
git add src/SysMonWidget/Converters/MetricStatusToBrushConverter.cs
git commit -m "refactor(converters): source status colors from Fluent palette instead of hardcoded brushes"
```

---

## Task 3: 위젯 카드 재구성 — 아크릴 느낌 + 케밥 헤더 분리

**Priority:** P0

**Files:**
- Modify: `src/SysMonWidget/Views/WidgetWindow.xaml`

**Interfaces:**
- Consumes: Task 1의 `WidgetSurfaceBrush`/`WidgetBorderBrush`/`TextPrimaryDarkBrush`/`TextSecondaryDarkBrush`, Task 2의 개선된 컨버터.
- `WidgetWindow.xaml.cs`는 변경 없음 (기존 `MenuButton_Click`/`OpenSettingsMenuItem_Click`/`HideWidgetMenuItem_Click`/`ExitMenuItem_Click`/`Window_MouseLeftButtonDown`/`Window_MouseRightButtonUp` 전부 그대로 재사용).

- [ ] **Step 1: `WidgetWindow.xaml` 전체 교체**

```xml
<Window x:Class="SysMonWidget.Views.WidgetWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:conv="clr-namespace:SysMonWidget.Converters"
        Title="SysMonWidget"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        Topmost="True"
        ShowInTaskbar="False"
        ResizeMode="NoResize"
        SizeToContent="WidthAndHeight"
        MouseLeftButtonDown="Window_MouseLeftButtonDown"
        MouseRightButtonUp="Window_MouseRightButtonUp">
    <Window.Resources>
        <conv:MetricStatusToBrushConverter x:Key="StatusToBrush" />
    </Window.Resources>
    <Border Padding="14,10,14,16" CornerRadius="14" MinWidth="160"
            Background="{StaticResource WidgetSurfaceBrush}"
            BorderBrush="{StaticResource WidgetBorderBrush}"
            BorderThickness="1"
            ToolTip="드래그하여 위치 이동 · 우클릭으로 상세 설정 열기"
            ToolTipService.InitialShowDelay="500">
        <Border.Effect>
            <DropShadowEffect Color="#000000" Opacity="0.35" BlurRadius="24" ShadowDepth="6" Direction="270" />
        </Border.Effect>
        <StackPanel>
            <Grid>
                <Button x:Name="MenuButton"
                        Content="⋮"
                        Width="22" Height="22"
                        Padding="0"
                        HorizontalAlignment="Right"
                        Background="Transparent"
                        BorderThickness="0"
                        Foreground="{StaticResource TextSecondaryDarkBrush}"
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

            <Border Height="1" Background="{StaticResource WidgetBorderBrush}" Margin="0,4,0,8" />

            <StackPanel>
                <StackPanel.Resources>
                    <Style TargetType="TextBlock" BasedOn="{StaticResource {x:Type TextBlock}}">
                        <Setter Property="FontSize" Value="15" />
                        <Setter Property="FontWeight" Value="SemiBold" />
                        <Setter Property="Foreground" Value="{StaticResource TextPrimaryDarkBrush}" />
                        <Setter Property="Margin" Value="0,3" />
                    </Style>
                </StackPanel.Resources>
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
        </StackPanel>
    </Border>
</Window>
```

(`{StaticResource {x:Type TextBlock}}`로 Task 1의 앱 레벨 `TextBlock` 암시적 스타일(FontFamily)을 `BasedOn`으로 체인 — 그냥 새 `Style`을 정의하면 앱 레벨 스타일을 덮어써서 FontFamily가 사라진다.)

- [ ] **Step 2: 빌드 확인**

Windows 쪽 복사 후 빌드. Expected: 빌드 성공.

- [ ] **Step 3: 수동 검증 (Windows에서 앱 실행)**

1. 카드 모서리가 실제로 둥글게 보이는지 (배경이 `Transparent`라 모서리 바깥 네 귀퉁이에 데스크톱이 살짝 비쳐야 함).
2. 케밥 버튼이 헤더 행에서 지표 목록과 구분선으로 분리돼 보이는지.
3. 위젯 본체 드래그 → 기존과 동일하게 이동하는지 (회귀 없음).
4. 위젯 본체 우클릭 → 팝업이 뜨는지 (회귀 없음).
5. 케밥 버튼 클릭 → 메뉴가 버튼 아래 뜨고, 설정 열기/숨기기/종료가 정상 동작하는지 (회귀 없음).
6. CPU/RAM/GPU/DISK/NET 색이 정상(초록)/경고(노랑)/위험(빨강)으로 잘 구분되는지, 어두운 카드 배경에서 잘 읽히는지.
7. 팝업에서 오퍼시티 슬라이더를 낮춰봤을 때 카드 전체가 자연스럽게 옅어지는지 (기존 `Window.Opacity` 메커니즘과 새 반투명 배경이 같이 작동, 회귀 없음).

- [ ] **Step 4: 커밋 (사용자 승인 후)**

```bash
git add src/SysMonWidget/Views/WidgetWindow.xaml
git commit -m "feat(widget): apply Fluent acrylic card style and move kebab menu into header row

기존엔 버튼이 카드 우상단에 절대좌표로 얹혀 있고 지표 목록이 오른쪽 여백만
비워두는 구조라 텍스트와 붙어 보였음. 호버로만 보이게/여백만 조정하는 대신
헤더 행을 분리해서 버튼 자리를 명시적으로 만드는 쪽을 택함."
```

---

## Task 4: 팝업 카드 + 컨트롤 재스타일

**Priority:** P1

**Files:**
- Modify: `src/SysMonWidget/Views/PopupWindow.xaml`

**Interfaces:**
- Consumes: Task 1 리소스, Task 2의 컨버터(`ConverterParameter=Light`).
- `PopupWindow.xaml.cs`는 변경 없음.

- [ ] **Step 1: `Window.Resources`에 컨트롤 스타일 추가 + 최상위를 카드 `Border`로 감싸기**

`src/SysMonWidget/Views/PopupWindow.xaml`을 다음으로 교체:

```xml
<Window x:Class="SysMonWidget.Views.PopupWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:conv="clr-namespace:SysMonWidget.Converters"
        Title="SysMonWidget - 상세"
        Width="320" SizeToContent="Height"
        Topmost="True"
        WindowStartupLocation="CenterScreen">
    <Window.Resources>
        <conv:MetricStatusToBrushConverter x:Key="StatusToBrush" />

        <Style TargetType="TextBox">
            <Setter Property="Background" Value="White" />
            <Setter Property="BorderBrush" Value="{StaticResource PopupBorderBrush}" />
            <Setter Property="BorderThickness" Value="1" />
            <Setter Property="Padding" Value="6,4" />
            <Setter Property="Foreground" Value="{StaticResource TextPrimaryLightBrush}" />
            <Setter Property="FontSize" Value="12" />
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="TextBox">
                        <Border Background="{TemplateBinding Background}"
                                BorderBrush="{TemplateBinding BorderBrush}"
                                BorderThickness="{TemplateBinding BorderThickness}"
                                CornerRadius="6">
                            <ScrollViewer x:Name="PART_ContentHost" Margin="{TemplateBinding Padding}" VerticalAlignment="Center" />
                        </Border>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <Style TargetType="CheckBox">
            <Setter Property="Foreground" Value="{StaticResource TextPrimaryLightBrush}" />
            <Setter Property="FontSize" Value="12" />
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="CheckBox">
                        <StackPanel Orientation="Horizontal">
                            <Border x:Name="Box" Width="16" Height="16" CornerRadius="4"
                                    BorderBrush="{StaticResource PopupBorderBrush}" BorderThickness="1"
                                    Background="Transparent" VerticalAlignment="Center">
                                <Path x:Name="Check" Data="M 3,8 L 6.5,11.5 L 13,4" Stroke="White" StrokeThickness="2"
                                      StrokeStartLineCap="Round" StrokeEndLineCap="Round" StrokeLineJoin="Round"
                                      Visibility="Collapsed" />
                            </Border>
                            <ContentPresenter Margin="8,0,0,0" VerticalAlignment="Center" />
                        </StackPanel>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsChecked" Value="True">
                                <Setter TargetName="Box" Property="Background" Value="{StaticResource AccentBrush}" />
                                <Setter TargetName="Box" Property="BorderBrush" Value="{StaticResource AccentBrush}" />
                                <Setter TargetName="Check" Property="Visibility" Value="Visible" />
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>

        <Style x:Key="PrimaryButtonStyle" TargetType="Button">
            <Setter Property="Background" Value="{StaticResource AccentBrush}" />
            <Setter Property="Foreground" Value="White" />
            <Setter Property="FontSize" Value="12" />
            <Setter Property="FontWeight" Value="SemiBold" />
            <Setter Property="Padding" Value="0,7" />
            <Setter Property="Cursor" Value="Hand" />
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border Background="{TemplateBinding Background}" CornerRadius="6">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" />
                        </Border>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </Window.Resources>

    <Border Background="{StaticResource PopupSurfaceBrush}" Padding="16">
        <StackPanel>
            <TextBlock Text="{Binding CpuUsagePercent, StringFormat='CPU 사용률: {0:F0}%'}"
                       Foreground="{Binding CpuStatus, Converter={StaticResource StatusToBrush}, ConverterParameter=Light}" FontSize="16" />
            <TextBlock Text="{Binding MemoryUsagePercent, StringFormat='메모리 사용률: {0:F0}%'}"
                       Foreground="{Binding MemoryStatus, Converter={StaticResource StatusToBrush}, ConverterParameter=Light}" FontSize="16" />
            <TextBlock Text="{Binding GpuUsagePercent, StringFormat='GPU 사용률: {0:F0}%'}"
                       Foreground="{Binding GpuStatus, Converter={StaticResource StatusToBrush}, ConverterParameter=Light}" FontSize="16" />
            <TextBlock Text="{Binding DiskUsagePercent, StringFormat='디스크 사용률: {0:F0}%'}"
                       Foreground="{Binding DiskStatus, Converter={StaticResource StatusToBrush}, ConverterParameter=Light}" FontSize="16" />
            <TextBlock Text="{Binding NetworkUsagePercent, StringFormat='네트워크 사용률: {0:F0}%'}"
                       Foreground="{Binding NetworkStatus, Converter={StaticResource StatusToBrush}, ConverterParameter=Light}" FontSize="16" />

            <Separator Margin="0,10" />
            <TextBlock Text="경고 임계값 (%)" FontWeight="Bold" Margin="0,0,0,6" />

            <Grid Margin="0,2">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="80" />
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="*" />
                </Grid.ColumnDefinitions>
                <TextBlock Text="CPU" VerticalAlignment="Center" />
                <TextBox x:Name="CpuWarningBox" Grid.Column="1" Margin="2" />
                <TextBox x:Name="CpuCriticalBox" Grid.Column="2" Margin="2" />
            </Grid>
            <Grid Margin="0,2">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="80" />
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="*" />
                </Grid.ColumnDefinitions>
                <TextBlock Text="메모리" VerticalAlignment="Center" />
                <TextBox x:Name="MemoryWarningBox" Grid.Column="1" Margin="2" />
                <TextBox x:Name="MemoryCriticalBox" Grid.Column="2" Margin="2" />
            </Grid>
            <Grid Margin="0,2">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="80" />
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="*" />
                </Grid.ColumnDefinitions>
                <TextBlock Text="GPU" VerticalAlignment="Center" />
                <TextBox x:Name="GpuWarningBox" Grid.Column="1" Margin="2" />
                <TextBox x:Name="GpuCriticalBox" Grid.Column="2" Margin="2" />
            </Grid>
            <Grid Margin="0,2">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="80" />
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="*" />
                </Grid.ColumnDefinitions>
                <TextBlock Text="디스크" VerticalAlignment="Center" />
                <TextBox x:Name="DiskWarningBox" Grid.Column="1" Margin="2" />
                <TextBox x:Name="DiskCriticalBox" Grid.Column="2" Margin="2" />
            </Grid>
            <Grid Margin="0,2">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="80" />
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="*" />
                </Grid.ColumnDefinitions>
                <TextBlock Text="네트워크" VerticalAlignment="Center" />
                <TextBox x:Name="NetworkWarningBox" Grid.Column="1" Margin="2" />
                <TextBox x:Name="NetworkCriticalBox" Grid.Column="2" Margin="2" />
            </Grid>

            <CheckBox x:Name="RunAtStartupCheckBox" Content="Windows 시작 시 자동 실행" Margin="0,10,0,0" />

            <Separator Margin="0,10" />
            <TextBlock Text="위젯 투명도" FontWeight="Bold" Margin="0,0,0,6" />
            <Slider x:Name="OpacitySlider" Minimum="0.3" Maximum="1.0"
                    TickFrequency="0.1" ValueChanged="OpacitySlider_ValueChanged" />

            <Button x:Name="SaveButton" Content="저장" Margin="0,14,0,0" Click="SaveButton_Click" Style="{StaticResource PrimaryButtonStyle}" />
            <TextBlock x:Name="SaveConfirmationText" Text="저장되었습니다"
                       Foreground="{StaticResource StatusNormalLightBrush}" Margin="0,6,0,0"
                       Visibility="Collapsed" />
        </StackPanel>
    </Border>
</Window>
```

(카드 `Border`는 `CornerRadius`를 주지 않고 전체를 채운다 — 네이티브 창 틀은 사각형이라 내부 요소만 둥글게 하면 창 모서리와 카드 모서리가 어긋나 보이는 문제를 아예 피하기 위함. `Slider` 스타일은 Task 5에서 추가.)

- [ ] **Step 2: 빌드 확인**

- [ ] **Step 3: 수동 검증**

1. 팝업이 밝은 카드로 보이는지, 텍스트/입력창 대비가 충분한지.
2. 임계값 텍스트박스에 값 입력 → 저장 클릭 → 기존과 동일하게 저장되는지 (회귀 없음).
3. 체크박스 클릭 → 체크 표시(흰색 체크마크 + 액센트 배경)가 정상 토글되는지.
4. 상태색(정상/경고/위험)이 밝은 배경에서 잘 읽히는지.

- [ ] **Step 4: 커밋 (사용자 승인 후)**

```bash
git add src/SysMonWidget/Views/PopupWindow.xaml
git commit -m "feat(popup): apply Fluent light card style to threshold editor controls"
```

---

## Task 5: 오퍼시티 슬라이더 커스텀 스타일

**Priority:** P1

**Files:**
- Modify: `src/SysMonWidget/Views/PopupWindow.xaml` (`Window.Resources`에 스타일 추가, `OpacitySlider`에 적용)

- [ ] **Step 1: `Window.Resources`에 슬라이더 스타일 추가**

Task 4의 `PrimaryButtonStyle` 뒤에 추가:

```xml
        <Style x:Key="AccentSliderStyle" TargetType="Slider">
            <Setter Property="Height" Value="20" />
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Slider">
                        <Grid VerticalAlignment="Center">
                            <Border Height="4" CornerRadius="2" Background="#E4E7EC" />
                            <Track x:Name="PART_Track">
                                <Track.DecreaseRepeatButton>
                                    <RepeatButton Command="Slider.DecreaseLarge">
                                        <RepeatButton.Template>
                                            <ControlTemplate TargetType="RepeatButton">
                                                <Border Height="4" CornerRadius="2" Background="{StaticResource AccentBrush}" />
                                            </ControlTemplate>
                                        </RepeatButton.Template>
                                    </RepeatButton>
                                </Track.DecreaseRepeatButton>
                                <Track.IncreaseRepeatButton>
                                    <RepeatButton Command="Slider.IncreaseLarge">
                                        <RepeatButton.Template>
                                            <ControlTemplate TargetType="RepeatButton">
                                                <Border Background="Transparent" />
                                            </ControlTemplate>
                                        </RepeatButton.Template>
                                    </RepeatButton>
                                </Track.IncreaseRepeatButton>
                                <Track.Thumb>
                                    <Thumb Width="14" Height="14">
                                        <Thumb.Template>
                                            <ControlTemplate TargetType="Thumb">
                                                <Ellipse Fill="White" Stroke="{StaticResource AccentBrush}" StrokeThickness="2" />
                                            </ControlTemplate>
                                        </Thumb.Template>
                                    </Thumb>
                                </Track.Thumb>
                            </Track>
                        </Grid>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
```

- [ ] **Step 2: `OpacitySlider`에 스타일 적용**

```xml
            <Slider x:Name="OpacitySlider" Minimum="0.3" Maximum="1.0"
                    TickFrequency="0.1" ValueChanged="OpacitySlider_ValueChanged"
                    Style="{StaticResource AccentSliderStyle}" />
```

- [ ] **Step 3: 빌드 확인**

- [ ] **Step 4: 수동 검증**

1. 슬라이더가 얇은 회색 트랙 + 액센트색 채움 + 원형 흰 손잡이로 보이는지.
2. 드래그하면 위젯 배경 투명도가 기존과 동일하게 실시간 반영되는지 (`OpacitySlider_ValueChanged` 로직 미변경이므로 회귀 없어야 함).
3. 종료 후 재실행 → 마지막 투명도가 복원되는지 (회귀 없음).

- [ ] **Step 5: 커밋 (사용자 승인 후)**

```bash
git add src/SysMonWidget/Views/PopupWindow.xaml
git commit -m "feat(popup): restyle opacity slider with accent-colored custom track and thumb"
```

---

## Task 6 (P2, 선택): 버튼 hover/press 피드백

**Priority:** P2 — 없어도 리디자인은 완결된 상태로 간주. 시간 여유가 있을 때만 진행.

`Style.Triggers`(`IsMouseOver`/`IsPressed`)로 위젯 케밥 버튼과 팝업 저장 버튼에 미세한 밝기 변화(`Opacity` 또는 배경색 소폭 변경)만 추가. 새 컨트롤/이벤트 없음.

## Task 7 (P2, 선택): 팝업 카드 안에 작은 타이틀 텍스트

**Priority:** P2 — 순수 장식. 없어도 지장 없음.

팝업 카드 맨 위, `Border Padding="16"` 안쪽 첫 줄에 `<TextBlock Text="SysMonWidget — 상세" FontWeight="SemiBold" FontSize="13" Margin="0,0,0,10" />` 추가.

---

## 전체 회귀 검증 (Task 1~5 완료 후)

Windows에서 `dotnet run --project src/SysMonWidget`으로 전체 시나리오를 한 번 더 통짜로 확인: 위젯 카드 표시 → 드래그 이동 → 케밥 메뉴(설정/숨기기/종료) → 우클릭 팝업 → 임계값 입력/저장 → 자동실행 체크박스 → 투명도 슬라이더 → 종료 후 재실행 시 위치/투명도/임계값 복원.

`dotnet test`로 기존 스위트(로직 미변경이므로 전부 그대로 통과해야 함) 재확인.
