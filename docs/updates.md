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

## 2026-09-27 — 정리 + Git 커밋/푸시

- `C:\dev\FolderSizeColumn`(1차 폐기 실험) 셸 확장 등록 해제 후 폴더 삭제.
- `FolderSizeViewer`를 git 저장소로 초기화, GitHub 비공개 저장소
  [dendr000/FolderSizeViewer](https://github.com/dendr000/FolderSizeViewer)로 푸시.
  빌드 산출물(`*.exe`, `*.pdb`, `*.obj`)은 `.gitignore`로 제외.

## 2026-09-27 — 버그 수정: 마우스 휠 스크롤 안 됨

- **증상**: 실행 파일에서 폴더 목록에 마우스 휠을 굴려도 스크롤이 되지 않음.
- **원인**: `ListView`를 별도 `ScrollViewer`로 한 번 더 감싸고 있었음. `ListView`는 기본
  템플릿에 자체 `ScrollViewer`를 이미 내장하고 있는데, 바깥쪽 `ScrollViewer`가 안쪽에
  무제한 높이를 줘버려서 안쪽 스크롤뷰어가 "스크롤할 내용 없음" 상태가 되고, 그 상태에서도
  휠 이벤트를 스스로 처리(Handled)해버려 바깥쪽으로 이벤트가 전달되지 않았음
  (ListView/ListBox/DataGrid를 ScrollViewer로 다시 감싸면 흔히 발생하는 WPF 함정).
- **수정**: 바깥쪽 `ScrollViewer` 래퍼를 제거하고 `ListView`가 자체 스크롤을 담당하도록 변경
  (`MainWindow.cs`의 `BuildListView()`).

## 2026-10-01 — 크기 계산을 "디스크 할당 크기" 기준으로 변경

- 사용자 피드백: 가상 디스크(VHD) 등 논리 크기와 실제 디스크 사용량이 크게 다른 파일이 있어
  크기가 왜곡되어 보인다는 지적. `DiskSize.cs` 신설, `GetCompressedFileSizeW` + 클러스터 크기
  올림으로 변경. 희소 파일(1GB 논리/0바이트 실사용)과 일반 파일(29바이트 → 4096바이트)로
  검증 완료. 자세한 내용: `docs/features/folder-size-calculation.md`,
  `docs/troubleshooting.md` 3번.
- "크기" 컬럼 이름을 "디스크 사용량"으로 변경.

## 2026-10-01 — `C:\dev\docs\guidelines` 필수 지침 체계 반영

- 이 세션에서 처음으로 `C:\dev\docs\guidelines\000 _start.md` 체계를 확인. 이 프로젝트가
  그동안 지침을 모른 채 진행되어 있었던 것을 소급 적용:
  - **010 code-rules 위반 발견 및 수정**: UI 아이콘을 `Icons.cs`/`MainWindow.cs`에 Geometry
    문자열로 직접 하드코딩하고 있었음 → `assets/icons/*.svg` 로 분리하고 `SvgIcon.cs`
    (직접 만든 최소 SVG 파서)로 읽어오도록 리팩터링.
  - **020 copyright 적용**: 모든 `.cs`/`.ps1` 파일 상단에 저작권 한 줄, 루트 `LICENSE`(MIT) 추가.
  - **030 completion-checklist 적용**: 루트 `CHANGELOG.md`(버전+시각 형식) 신설,
    `docs/filetree.md`·`docs/features/`·`docs/troubleshooting.md`·`README.md` 신설.
  - `CLAUDE.md` 신설 (필수 지침 루트를 가리키는 첫 줄 포함).
- **주의**: `docs/updates.md`(이 파일, 서사적 개발 일지)와 루트 `CHANGELOG.md`(버전 번호가 붙은
  간결한 변경 로그)는 역할이 다르다 — 앞으로도 코드가 바뀌면 **둘 다** 갱신한다.

## 2026-10-01 — 버그 수정: 기본 정렬이 뒤죽박죽으로 보임

- **증상**: 사용자 피드백 — 폴더 안 폴더/파일 순서가 뒤죽박죽이고, 기본적으로 이름순이면
  좋겠다는 요청.
- **원인**: 기본 정렬 기준이 "크기 내림차순"(`_sortProperty = "SizeBytes"`)으로 되어 있었고,
  드라이브 목록 화면(`LoadDriveList`)이 자기 화면을 보여줄 때마다 이 **공유된** 정렬 상태를
  강제로 "크기 내림차순"으로 덮어쓰고 있었음. 그래서 드라이브 목록 → 아무 드라이브나 진입
  → 그 드라이브의 첫 폴더 뷰까지 전부 크기순으로 시작되어, 이름순을 기대한 사용자 눈에는
  순서가 종잡을 수 없게 보였음. 거기에 더해 폴더 크기는 백그라운드에서 서서히 계산되며
  값이 채워지니, 크기순 정렬 상태에서는 계산되는 동안 행 순서가 계속 들썩이는 문제까지 있었음.
- **수정**:
  1. 기본 정렬을 **이름 오름차순**으로 변경, 드라이브 목록이 더 이상 정렬 상태를 강제로
     덮어쓰지 않도록 수정 — 이제 드라이브 목록도 기본은 이름순이고, 사용자가 "디스크 사용량"
     헤더를 클릭하면 그 선택이 드라이브 목록과 폴더 뷰 모두에 일관되게 적용됨.
  2. 정렬 기준이 무엇이든 **폴더가 항상 파일보다 위**에 오도록 `IsDirectory` 내림차순을
     1차 정렬 기준으로 추가 (탐색기와 동일한 관례).
  3. `RowItem.IsDirectory`를 필드에서 프로퍼티로 변경 — WPF의 `SortDescription`은
     `TypeDescriptor` 기반이라 필드는 정렬 기준으로 인식하지 못하고 예외가 남.
- 자세한 내용: `docs/troubleshooting.md` 5번, `docs/features/navigation.md`.

## 다음에 할 일 / 주의할 점

- 사용자가 실제로 앱을 열어보고 라이트/다크 전환·애니메이션·정렬·탐색·새 아이콘이 기대대로
  동작하는지 확인 필요.
- 새 코드 파일을 추가하면 `000 _start.md` 3장 "지침 파일 목록"에 등록하는 규칙은 이 프로젝트
  코드가 아니라 지침 저장소(`C:\dev\docs\guidelines`) 쪽 작업이니 혼동하지 말 것.
