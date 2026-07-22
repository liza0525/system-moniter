# SysMonWidget UX 개선: 팝업 저장 확인 텍스트 — 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

## Context

팝업(`PopupWindow`)에서 임계값/자동실행/투명도를 수정하고 "저장" 버튼을 눌러도, 저장이 실제로 됐는지 알 수 있는 방법이 없다 — 버튼을 눌러도 아무 반응이 없어 보인다. 사용자 요구: 저장 버튼을 누르면 (a) 저장 후 팝업이 자동으로 닫히거나, (b) "저장되었습니다" 알림이 뜨는 것 중 하나는 있어야 한다. 브레인스토밍 결과 (b)로 확정 — 팝업에는 임계값 5종 + 자동실행 + 투명도 슬라이더가 함께 있어서, 저장 후 자동으로 닫히면 다른 설정을 이어서 조정하기 불편하기 때문. 창을 유지한 채 간단한 확인 텍스트만 보여주는 쪽을 선택했다.

이 작업은 같은 세션에서 진행 중인 "위젯 메뉴 버튼" 작업([2026-07-22-widget-menu-button.md](2026-07-22-widget-menu-button.md))과 무관한 별개 변경이라 계획서/커밋을 분리한다.

## 설계 결정 및 근거

- **`PopupWindow.xaml`의 `SaveButton` 아래에 `TextBlock`을 하나 추가하고, 기본값은 `Visibility="Collapsed"`로 둔다.** `SaveButton_Click`의 마지막 줄에서 `Visibility = Visibility.Visible`로 바꾸기만 하면 된다.
- **자동 숨김 타이머는 넣지 않는다.** `App.xaml.cs`의 `OnTogglePopupRequested`는 팝업을 열 때마다 `new PopupWindow(...)`로 항상 새 인스턴스를 만든다 — 즉 팝업을 다시 열면 이 텍스트는 자동으로 `Collapsed` 상태로 초기화된다. 따라서 "이전 저장 확인이 계속 남아있다" 같은 문제가 애초에 생기지 않고, `DispatcherTimer` 같은 추가 로직도 필요 없다 (YAGNI).
- **저장 로직 자체는 전혀 바꾸지 않는다.** 기존 `SaveButton_Click`의 임계값 파싱/저장, `StartupRegistrationService` 호출, `ThresholdsSaved` 이벤트 발생 순서는 그대로 두고, 맨 끝에 한 줄만 추가한다.
- **`App.xaml.cs`, `WidgetWindow`, `TrayIconManager`는 전혀 건드리지 않는다.** `PopupWindow` 내부에서만 완결되는 변경이다.

## Global Constraints

- Windows 전용(WPF, `net8.0-windows`). WSL에서 개발하고 `/mnt/c/Users/new25/D/pjt/system-monitor`(동일 git remote `liza0525/system-moniter`)로 수정 파일을 복사해 `"/mnt/c/Program Files/dotnet/dotnet.exe"`로 빌드/실행 검증한다.
- 빌드 전 `SysMonWidget.exe`가 실행 중이면 자기 바이너리를 잠그므로 `taskkill.exe`로 종료 후 재빌드한다.
- XAML 뷰는 자동 단위테스트 대상이 아니다 (CLAUDE.md 기존 방침) — 수동 실행 검증으로 대체.
- 기존 저장 동작(임계값 파싱/반영, 자동실행 등록, `ThresholdsSaved` 이벤트)은 회귀 없이 그대로 유지되어야 한다.
- **커밋은 사용자가 명시적으로 지시할 때만 진행한다** (CLAUDE.md 정책).
- **구현은 이 계획이 승인된 직후 자동으로 시작하지 않는다.** 사용자가 별도로 "시작해"라고 지시할 때까지 대기한다.

---

## 우선순위 개요

| 우선순위 | 의미 | 포함 Task |
|---|---|---|
| **P0** | 이번 요청의 전부 — 단일 Task, 조건부/후속 작업 없음 | 1 |

**근거:** 이번 작업은 `PopupWindow` 안에서 완결되는 단일 변경이라 별도로 나눌 하위 작업이 없다. 굳이 우선순위를 나눈다면 전체가 P0.

---

## Task 1: 저장 확인 텍스트 추가

**Priority:** P0

**Files:**
- Modify: `src/SysMonWidget/Views/PopupWindow.xaml`
- Modify: `src/SysMonWidget/Views/PopupWindow.xaml.cs`

**Interfaces:**
- Consumes: 없음.
- Produces: 없음 (외부에서 참조할 새 public 멤버 없음 — `App.xaml.cs` 변경 불필요).

- [ ] **Step 1: `PopupWindow.xaml`에 확인 텍스트 추가**

`SaveButton` 바로 뒤에 추가:

```xml
        <Button x:Name="SaveButton" Content="저장" Margin="0,10,0,0" Click="SaveButton_Click" />
        <TextBlock x:Name="SaveConfirmationText" Text="저장되었습니다"
                   Foreground="#4CAF50" Margin="0,6,0,0"
                   Visibility="Collapsed" />
```

- [ ] **Step 2: `PopupWindow.xaml.cs`의 `SaveButton_Click` 끝에 한 줄 추가**

```csharp
        ThresholdsSaved?.Invoke(this, _thresholds);

        SaveConfirmationText.Visibility = Visibility.Visible;
    }
```

(`ThresholdsSaved?.Invoke(this, _thresholds);` 바로 뒤, 메서드를 닫는 `}` 앞에 추가하는 것.)

- [ ] **Step 3: Windows 쪽 복사 + 빌드 확인**

```bash
cp src/SysMonWidget/Views/PopupWindow.xaml /mnt/c/Users/new25/D/pjt/system-monitor/src/SysMonWidget/Views/PopupWindow.xaml
cp src/SysMonWidget/Views/PopupWindow.xaml.cs /mnt/c/Users/new25/D/pjt/system-monitor/src/SysMonWidget/Views/PopupWindow.xaml.cs
```

실행 중인 `SysMonWidget.exe`가 있으면 먼저 종료 (`/mnt/c/Windows/System32/taskkill.exe /IM SysMonWidget.exe /F`), 이후:

```bash
cd /mnt/c/Users/new25/D/pjt/system-monitor && "/mnt/c/Program Files/dotnet/dotnet.exe" build src/SysMonWidget
```

Expected: 빌드 성공, 오류 0개.

- [ ] **Step 4: 수동 검증 (Windows에서 앱 실행)**

1. 팝업을 열었을 때 "저장되었습니다" 텍스트가 보이지 않는지 확인 (초기 상태 `Collapsed`).
2. 임계값 값을 하나 바꾸고 "저장" 클릭 → 즉시 "저장되었습니다" 텍스트가 나타나는지 확인.
3. 팝업이 닫히지 않고 그대로 열려 있는지 확인 (자동 닫힘 없음).
4. 팝업을 닫고 다시 열었을 때 "저장되었습니다" 텍스트가 다시 안 보이는지 확인 (새 인스턴스라 `Collapsed`로 초기화됨).
5. 같은 팝업에서 "저장"을 여러 번 눌러도 정상 동작하는지(임계값 회귀 없음), 자동실행 체크박스/투명도 슬라이더 동작에 영향이 없는지 확인.

- [ ] **Step 5: 커밋 (사용자 승인 후)**

```bash
git add src/SysMonWidget/Views/PopupWindow.xaml src/SysMonWidget/Views/PopupWindow.xaml.cs
git commit -m "feat(popup): show confirmation text after saving settings"
```
