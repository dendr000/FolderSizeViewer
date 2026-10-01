# 폴더 크기 보기 (FolderSizeViewer)

Windows 탐색기가 보여주지 않는 **폴더 크기**를 보여주는 독립 실행형 유틸리티.
파일은 탐색기의 "크기" 열에 바로 보이지만 폴더는 하위 파일을 전부 더한 크기가 표시되지
않는데, 이 앱은 드라이브/폴더를 탐색하면서 각 폴더의 크기를 계산해 보여준다.

## 기능

- 드라이브 목록 → 폴더 탐색 → 상위 폴더 이동, 경로 직접 입력, 찾아보기 대화상자
- 폴더 크기를 백그라운드에서 재귀적으로 계산 (UI 멈추지 않음, 폴더 이동 시 이전 계산 취소)
- **디스크 할당 크기 기준**으로 계산 — 가상 디스크(VHD)나 희소 파일처럼 논리 크기와 실제
  사용량이 크게 다른 경우도 정확하게 표시 (자세히: [docs/features/folder-size-calculation.md](docs/features/folder-size-calculation.md))
- 이름·종류·크기 기준 정렬, 크기 열에 상대적 크기를 보여주는 막대 표시
- 라이트/다크 테마 (시스템 설정 자동 감지 + 수동 전환, 부드러운 크로스페이드)
- 폴더 이동 시 페이드인, 계산 중 스피너 회전 등 모션
- 우클릭 → 탐색기에서 열기, 더블클릭 → 폴더 진입/파일은 기본 프로그램으로 열기

삭제·이동 등 파일 관리 기능은 의도적으로 넣지 않았다. 이 도구는 **보기 전용**이다.

## 빌드 방법

Visual Studio나 .NET SDK 설치가 필요 없다. Windows에 기본 내장된
`.NET Framework` 컴파일러(`csc.exe`)만으로 빌드한다.

```powershell
.\build.ps1
```

`FolderSizeViewer.exe` 가 같은 폴더에 생성된다.

## 실행 방법

```powershell
.\FolderSizeViewer.exe
```

또는 탐색기에서 `FolderSizeViewer.exe` 를 더블클릭. 인자로 시작 폴더 경로를 줄 수도 있다:

```powershell
.\FolderSizeViewer.exe "D:\Sub"
```

코드 서명을 하지 않았으므로 처음 실행할 때 Windows SmartScreen 경고가 뜰 수 있다
("추가 정보" → "실행" 으로 진행).

## 기술 스택

- WPF, .NET Framework (C# 5 문법 제약 — `csc.exe`가 그 이상을 지원하지 않음)
- XAML/BAML 컴파일러 없이 UI를 전부 C# 코드로 구성
- UI 아이콘은 `assets/icons/*.svg` 로 분리하고 자체 제작한 최소 SVG 파서(`SvgIcon.cs`)로 로드

## 문서

- [docs/plan.md](docs/plan.md) — 계획서 (기술 조사 과정, 왜 이 방식을 택했는지)
- [docs/updates.md](docs/updates.md) — 개발 일지
- [CHANGELOG.md](CHANGELOG.md) — 버전별 변경 내역
- [docs/troubleshooting.md](docs/troubleshooting.md) — 겪은 문제와 해결 기록
- [docs/features/](docs/features/) — 기능별 설명

## 라이선스

MIT License, Copyright (c) 2026 dendr000. 자세한 내용은 [LICENSE](LICENSE) 참고.
