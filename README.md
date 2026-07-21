# SysMonWidget

Windows 데스크톱에서 CPU / 메모리 / GPU / 디스크 / 네트워크 사용률을 실시간으로 보여주는 항상-위(Always-on-top) 모니터링 위젯입니다.

## 프로젝트 소개

작업관리자를 열지 않아도 화면 한 켠에서 시스템 부하를 바로 확인할 수 있도록 만든 개인용 위젯입니다. 평소엔 숫자만 간결하게 보여주는 작은 위젯으로 떠 있다가, 지표가 임계값을 넘으면 색상으로 경고해주고, 우클릭하면 임계값을 직접 조정할 수 있는 확장 뷰가 뜹니다.

자세한 요구사항은 [PRD.md](PRD.md), 프로젝트 구조와 개발 시 유의사항은 [CLAUDE.md](CLAUDE.md)를 참고하세요.

## 기술 스택

- C# / .NET 8 (WPF)
- `System.Diagnostics.PerformanceCounter` — CPU/메모리/GPU/디스크/네트워크 지표 수집
- `System.Text.Json` — 설정 저장/로드
- `Microsoft.Win32.Registry` — Windows 시작 시 자동 실행 등록
- WinForms `NotifyIcon` — 시스템 트레이 아이콘
- xUnit — 단위 테스트

## 주요 기능

- **항상-위 미니 위젯**: CPU/RAM/GPU/디스크/네트워크 사용률을 숫자로 표시, 드래그로 위치 이동
- **임계값 색상 경고**: 지표별로 경고(노랑)/위험(빨강) 기준을 넘으면 색이 바뀜 (기본값 있음, 직접 조정 가능)
- **팝업 확장 뷰**: 위젯 우클릭 시 더 자세한 수치와 임계값 편집 화면 표시
- **설정 자동 저장**: 위젯 위치·투명도·임계값을 기억했다가 재실행 시 복원
- **Windows 시작 시 자동 실행**: 옵션으로 켜고 끌 수 있음
- **트레이 아이콘**: 좌클릭으로 위젯 표시/숨김, 우클릭 메뉴로 종료

## 로컬 실행 방법

Windows 전용 WPF 앱이라 실행하려면 **.NET 8 SDK**와 **Windows**가 필요합니다.

```bash
# 빌드
dotnet build

# 실행 (개발 모드)
dotnet run --project src/SysMonWidget

# 테스트
dotnet test

# 배포용 단일 exe 생성
dotnet publish src/SysMonWidget -c Release
```

퍼블리시 결과물은 `src/SysMonWidget/bin/Release/net8.0-windows/win-x64/publish/SysMonWidget.exe`에 생성되며, .NET 런타임 설치 없이 바로 실행할 수 있습니다.

> Git Bash 등 bash 계열 셸을 쓴다면 `--project` 경로에 백슬래시(`\`) 대신 슬래시(`/`)를 쓰세요. 자세한 내용은 [CLAUDE.md](CLAUDE.md) 참고.
