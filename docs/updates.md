# 업데이트 내역

## 2026-09-26 — 1차 시도: IColumnProvider 셸 확장 (폐기)

- **무엇을**: 탐색기에 "폴더 크기" / "폴더 크기(바이트)" 컬럼을 추가하는 COM 셸 확장을
  C# + .NET Framework(csc.exe, RegAsm)로 구현. `C:\dev\FolderSizeColumn`.
- **검증**: 직접 COM 활성화 테스트(`GetColumnInfo`, `GetItemData`)는 전부 정상 동작(HR=0,
  크기 값도 정확히 계산됨). 그러나 실제 탐색기의 "세부 정보 선택" 대화상자에는 나타나지 않음.
- **원인**: `explorer.exe` 프로세스에 `mscoree`/`clr` 모듈이 전혀 로드되지 않은 것을 확인 —
  탐색기가 우리 COM 객체를 아예 호출조차 하지 않았다는 뜻. Microsoft 공식 문서 확인 결과
  `IColumnProvider`는 **Windows XP까지만 지원**되고 Vista 이후 제거된 API였음.
- **결정**: 이 방식은 폐기. Vista 이후 대체 수단(Property System/PropertyHandler)은 폴더에
  대한 동작 검증 사례가 부족하고 구현 난이도 대비 실패 위험이 커서, 사용자와 상의 후
  독립 실행형 프로그램으로 전환하기로 결정.

## 2026-09-26 — 2차: WinForms 독립 프로그램

- **무엇을**: 드라이브 목록 → 폴더 탐색 → 재귀적 크기 계산(백그라운드 스레드, 취소 가능)을
  갖춘 WinForms 앱 1차 버전 작성 (`FolderSizeViewer.exe`).
- **디버깅 메모**: 최초 실행 시 창이 뜨지 않는 것처럼 보였던 문제가 있었음 — 콘솔 서브시스템으로
  다시 빌드해 디버그 로그를 찍어본 결과 생성자 로직 자체는 문제없이 끝까지 실행됨을 확인.
  이후 재실행에서는 정상적으로 창이 뜸 (최초 1회성 지연은 새로 컴파일된 서명되지 않은 exe에 대한
  백그라운드 스캔/보안 검사 등 환경 요인으로 추정, 코드 버그 아님).

## 2026-09-27 — 3차: WPF로 전환 (디자인 요청 반영)

- **무엇을**: 사용자가 "디자인 신경 써서, 라이트/다크 모드, 애니메이션/모션"을 요청.
  WinForms는 테마·애니메이션 구현에 한계가 커서 **WPF로 전면 재작성**.
- **구현**:
  - `Theme.cs` — 시스템 다크모드 자동 감지 + 공유 `SolidColorBrush` 인스턴스를 애니메이션시켜
    라이트/다크 전환 시 전체 UI가 부드럽게 크로스페이드.
  - `Icons.cs` — 외부 이미지 없이 `Geometry`/`Path`/도형 조합만으로 직접 그린 아이콘
    (폴더, 파일, 위/새로고침/검색 화살표, 해/달, 드라이브).
  - `MainWindow.cs` — XAML 없이 전부 코드로 구성한 UI (`FrameworkElementFactory` 기반
    `ControlTemplate`/`DataTemplate`). 플랫 리스트, 라운드 코너, 호버/선택 하이라이트,
    크기 컬럼 상대 막대바, 폴더 이동 시 페이드인, 크기 계산 중 스피너 회전 애니메이션.
  - `RowItem.cs` — `INotifyPropertyChanged` 기반 뷰모델로 배경 스캔 진행 중에도 UI 실시간 갱신.
- **빌드 환경 이슈 및 해결**:
  - `System.IO.Path`와 `System.Windows.Shapes.Path` 네임스페이스 충돌 → 전체 경로로 명시.
  - `csc.exe`가 C# 5까지만 지원 → `?.` 연산자 등 최신 문법 전부 구식 문법으로 교체.
  - WPF 어셈블리(`PresentationCore`/`PresentationFramework`/`WindowsBase`)와 `System.Xaml`은
    GAC/`Framework64\v4.0.30319\WPF` 경로를 직접 지정해서 참조.
- **검증**: `user32.dll` `EnumWindows`로 실제 창 핸들과 타이틀("폴더 크기 보기")이 정상 표시됨을
  확인. 사용자 육안 확인은 별도 요청 예정 (에이전트가 데스크톱 스크린샷을 직접 찍을 수단이 없음).

## 다음에 할 일 / 주의할 점

- 사용자가 실제로 앱을 열어보고 라이트/다크 전환·애니메이션·정렬·탐색이 기대대로 동작하는지
  확인 필요.
- `C:\dev\FolderSizeColumn`(1차 폐기 실험)은 레지스트리에 무해하게 남아있음 — 정리 원하면
  `unregister.ps1` 실행 후 폴더 삭제.
