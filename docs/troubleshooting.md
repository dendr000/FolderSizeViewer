# 트러블슈팅 기록

## 1. 탐색기 "세부 정보 선택" 목록에 커스텀 컬럼이 안 뜸

- **증상**: `IColumnProvider` 셸 확장을 구현·등록했는데, 탐색기 우클릭 → "자세히" → "더 보기..."
  목록에 추가한 컬럼이 전혀 보이지 않음.
- **원인**: 직접 COM 활성화 테스트(`GetColumnInfo`, `GetItemData` 호출)는 전부 정상 동작했지만,
  `explorer.exe` 프로세스에 `.NET` 런타임(`mscoree`/`clr` 모듈)이 전혀 로드되지 않은 것으로 보아
  탐색기가 등록된 COM 객체를 아예 호출조차 하지 않았음을 확인. Microsoft 공식 문서
  (`learn.microsoft.com/.../lwef/column-handlers`)에 **"Windows XP 이하에서만 지원"**이라고
  명시되어 있었음 — Vista 이후 제거된 API.
- **해결**: 이 방식을 폐기하고 독립 실행형 WPF 프로그램으로 전환.

## 2. 실행 파일에서 마우스 휠 스크롤이 안 됨

- **증상**: 빌드한 `.exe`에서 폴더 목록에 마우스 휠을 굴려도 스크롤되지 않음.
- **원인**: `ListView`를 `ScrollViewer`로 한 번 더 감싸고 있었음. `ListView`는 기본 템플릿에
  자체 `ScrollViewer`를 이미 내장하는데, 바깥 `ScrollViewer`가 안쪽에 무제한 높이를 줘버려서
  안쪽 스크롤뷰어가 "스크롤할 내용 없음" 상태가 되고, 그 상태에서도 휠 이벤트를 자기가
  처리(Handled)해버려 바깥으로 이벤트가 전달되지 않음. (`ListView`/`ListBox`/`DataGrid`를
  `ScrollViewer`로 또 감싸면 흔히 생기는 WPF 함정.)
- **해결**: 바깥쪽 `ScrollViewer` 래퍼를 제거하고 `ListView`가 자체 스크롤을 담당하도록 변경
  (`MainWindow.cs`의 `BuildListView()`).

## 3. 폴더 크기가 비정상적으로 크게 나옴 (가상 디스크 등)

- **증상**: 가상 디스크(VHD/VHDX) 파일이 들어있는 폴더의 크기가 실제 디스크 사용량과 전혀
  맞지 않게 크게(예: 500GB) 표시됨.
- **원인**: `FileInfo.Length`(논리 크기)를 그대로 더하고 있었음. 동적으로 커지는 가상 디스크나
  희소 파일(sparse file)은 논리 크기와 실제 디스크 사용량이 크게 다를 수 있다.
- **해결**: `GetCompressedFileSizeW` + 클러스터 크기 올림으로 "디스크 할당 크기" 기준으로
  계산하도록 변경 (`DiskSize.cs`). 자세한 내용은 `docs/features/folder-size-calculation.md`.

## 4. 폴더 안 항목 순서가 뒤죽박죽으로 보임

- **증상**: 폴더를 열 때마다 기본 정렬이 이름순이 아니라 뒤죽박죽으로 보이고, 폴더 크기가
  계산되는 동안 행 순서가 계속 바뀜.
- **원인**: 기본 정렬 기준(`_sortProperty`/`_sortAscending`)이 드라이브 목록 화면과 폴더 뷰가
  **같은 필드를 공유**하고 있었는데, 드라이브 목록(`LoadDriveList`)이 자기 화면을 그릴 때마다
  이 공유 상태를 "크기 내림차순"으로 강제로 덮어쓰고 있었음. 그래서 드라이브 → 폴더로 들어갈
  때마다 폴더 뷰도 크기순으로 시작됐고, 폴더 크기는 백그라운드에서 서서히 채워지니 그동안
  행 순서가 계속 들썩였음.
- **해결**: 기본 정렬을 이름 오름차순으로 바꾸고, 드라이브 목록이 정렬 상태를 더 이상 강제로
  덮어쓰지 않게 함. 추가로 정렬 기준과 무관하게 폴더가 항상 파일보다 위에 오도록
  `IsDirectory` 내림차순을 1차 정렬 기준으로 넣었다 (`MainWindow.cs`의 `ApplySort()`).
- **곁들여 고친 것**: `RowItem.IsDirectory`가 필드라서 `SortDescription`에 못 썼다
  (WPF의 정렬은 `TypeDescriptor` 기반이라 프로퍼티만 인식, 필드는 예외가 남) — 프로퍼티로 변경.

## 5. `System.IO.Path` 와 `System.Windows.Shapes.Path` 이름 충돌

- **증상**: `using System.IO;` 와 `using System.Windows.Shapes;` 를 동시에 쓰면 `Path` 라는
  이름을 쓸 때마다 `CS0104: 'Path'은(는) ... 모호한 참조입니다` 컴파일 오류가 남.
- **원인**: 두 네임스페이스에 똑같이 `Path` 라는 타입이 있음 (`System.IO.Path`는 정적 유틸리티
  클래스, `System.Windows.Shapes.Path`는 도형 컨트롤). 한쪽을 완전한 이름으로 안 써주면
  파일 전체에서 `Path`를 쓰는 모든 자리가 모호해진다.
- **해결**: 도형 쪽은 항상 `System.Windows.Shapes.Path`로 전체 경로를 써서 구분
  (`MainWindow.cs`, `SvgIcon.cs`). `System.IO.Path.GetFileName`/`GetExtension`처럼 파일 경로
  쪽도 모호해지는 자리는 마찬가지로 전체 경로로 명시.
