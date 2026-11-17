# 파일 구조

```
FolderSizeViewer/
├── CLAUDE.md               # 프로젝트 지침 (필수 지침 루트를 가리킴)
├── LICENSE                 # MIT, Copyright (c) 2026 dendr000
├── CHANGELOG.md            # 버전별 변경 내역 (버전+시각)
├── README.md               # 실행·빌드 방법, 기능 목록
├── build.ps1               # OS 내장 csc.exe 로 빌드하는 스크립트 (SVG 아이콘 리소스 임베드 포함)
├── Program.cs              # 진입점 ([STAThread] Main, Application 생성)
├── MainWindow.Menu.cs      # 상단 메뉴 바, 삭제 가능 폴더 목록, 단축키 (MainWindow 의 일부)
├── MainWindow.Favorites.cs # 삭제 가능 폴더 전체 목록 창 (검색, 이동, 표시 해제)
├── MenuStyles.cs           # 메뉴·우클릭 메뉴를 테마 색으로 그리는 모양
├── AssemblyInfo.cs         # 제품명·버전 (정보 창과 exe 파일 속성에 쓰임)
├── MainWindow.cs           # 전체 UI 구성(XAML 없이 코드로) + 탐색/정렬/백그라운드 스캔 로직
├── Theme.cs                # 라이트/다크 팔레트, 시스템 다크모드 감지, 전환 애니메이션
├── Icons.cs                # SvgIcon 로더를 아이콘 이름별로 감싸는 얇은 창구
├── SvgIcon.cs               # assets/icons/*.svg 를 읽어 WPF Geometry/Shape로 변환하는 최소 파서
├── RowItem.cs              # 목록 한 줄을 표현하는 INotifyPropertyChanged 뷰모델
├── Converters.cs           # 바인딩 값 변환기 (크기→막대 너비, bool→Visibility)
├── DeletableMarks.cs       # "삭제해도 되는 폴더" 표시를 경로 기준으로 앱 안에 저장 (파일은 건드리지 않음)
├── SizeCache.cs            # 한 번 계산한 폴더 크기를 기억해 다음에 바로 보여 주기 위한 저장소
├── DiskSize.cs             # 파일의 "디스크 할당 크기" 계산 (GetCompressedFileSizeW 기반)
├── tools/
│   └── make-icon.ps1       # assets/app.ico 를 다시 만드는 도구
├── assets/
│   ├── app.ico             # 앱 아이콘 (16~256px, exe 와 창 아이콘에 사용)
│   └── icons/              # UI 아이콘 원본 (svg). 코드에 아이콘 마크업을 직접 넣지 않는다.
│       ├── folder.svg
│       ├── file.svg
│       ├── arrow-up.svg
│       ├── refresh.svg
│       ├── search.svg
│       ├── sun.svg
│       ├── moon.svg
│       └── drive.svg
├── tests/
│   ├── test.ps1            # 임시 폴더에서 시험을 컴파일·실행 (실제 데이터 미사용)
│   ├── DeletableMarksTests.cs
│   └── SizeCacheTests.cs
└── docs/
    ├── plan.md             # 계획서 — 기술 조사 과정, 아키텍처·디자인 결정 이유
    ├── updates.md          # 개발 일지 (서사적 기록, CHANGELOG.md의 상세 버전)
    ├── filetree.md         # 이 파일
    ├── troubleshooting.md  # 증상/원인/해결 기록
    ├── plans/
    │   ├── 01-deletable-marks.md  # 삭제 가능 표시·크기 기억 계획서
    │   └── 02-menu-bar-and-favorites.md  # 메뉴 바·삭제 가능 바로가기 계획서
    └── features/           # 기능별 설명 문서
        ├── app-icon.md
        ├── deletable-marks.md
        ├── folder-size-calculation.md
        ├── menu-bar.md
        ├── navigation.md
        └── theme-and-motion.md
```

빌드 산출물(`FolderSizeViewer.exe`, `*.pdb`, `*.obj`)은 `.gitignore`로 제외하고 git에 커밋하지 않는다.
