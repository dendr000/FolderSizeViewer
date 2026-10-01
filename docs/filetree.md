# 파일 구조

```
FolderSizeViewer/
├── CLAUDE.md               # 프로젝트 지침 (필수 지침 루트를 가리킴)
├── LICENSE                 # MIT, Copyright (c) 2026 dendr000
├── CHANGELOG.md            # 버전별 변경 내역 (버전+시각)
├── README.md               # 실행·빌드 방법, 기능 목록
├── build.ps1               # OS 내장 csc.exe 로 빌드하는 스크립트 (SVG 아이콘 리소스 임베드 포함)
├── Program.cs              # 진입점 ([STAThread] Main, Application 생성)
├── MainWindow.cs           # 전체 UI 구성(XAML 없이 코드로) + 탐색/정렬/백그라운드 스캔 로직
├── Theme.cs                # 라이트/다크 팔레트, 시스템 다크모드 감지, 전환 애니메이션
├── Icons.cs                # SvgIcon 로더를 아이콘 이름별로 감싸는 얇은 창구
├── SvgIcon.cs               # assets/icons/*.svg 를 읽어 WPF Geometry/Shape로 변환하는 최소 파서
├── RowItem.cs              # 목록 한 줄을 표현하는 INotifyPropertyChanged 뷰모델
├── Converters.cs           # 바인딩 값 변환기 (크기→막대 너비, bool→Visibility)
├── DiskSize.cs             # 파일의 "디스크 할당 크기" 계산 (GetCompressedFileSizeW 기반)
├── assets/
│   └── icons/              # UI 아이콘 원본 (svg). 코드에 아이콘 마크업을 직접 넣지 않는다.
│       ├── folder.svg
│       ├── file.svg
│       ├── arrow-up.svg
│       ├── refresh.svg
│       ├── search.svg
│       ├── sun.svg
│       ├── moon.svg
│       └── drive.svg
└── docs/
    ├── plan.md             # 계획서 — 기술 조사 과정, 아키텍처·디자인 결정 이유
    ├── updates.md          # 개발 일지 (서사적 기록, CHANGELOG.md의 상세 버전)
    ├── filetree.md         # 이 파일
    ├── troubleshooting.md  # 증상/원인/해결 기록
    └── features/           # 기능별 설명 문서
        ├── folder-size-calculation.md
        ├── navigation.md
        └── theme-and-motion.md
```

빌드 산출물(`FolderSizeViewer.exe`, `*.pdb`, `*.obj`)은 `.gitignore`로 제외하고 git에 커밋하지 않는다.
