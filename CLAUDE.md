필수 지침: "C:\dev\docs\guidelines\000 _start.md" 를 먼저 읽는다.

# 폴더 크기 보기 (FolderSizeViewer)

탐색기가 보여주지 않는 폴더 크기를 보여주는 독립 실행형 WPF 유틸리티.

## 기술 스택

- WPF, .NET Framework, C# 5 문법 제약 (Visual Studio·`.NET SDK` 미설치 환경이라
  OS 내장 `csc.exe` 만으로 빌드 — `build.ps1` 참고).
- XAML/BAML 컴파일러 없이 빌드하므로 UI는 전부 C# 코드로 구성 (`FrameworkElementFactory`).

## 절대 규칙

1. **한국어로 응답한다.**
2. UI 아이콘은 `assets/icons/*.svg` 로 분리하고 `SvgIcon.cs` 로 읽어온다. 코드에 SVG/Geometry
   문자열을 직접 넣지 않는다.
3. `System.IO.Path` 와 `System.Windows.Shapes.Path` 이름이 겹치니 항상 전체 경로로 구분해서 쓴다.
4. 삭제·이동 등 파일 관리 기능은 넣지 않는다 — 이 도구는 "보기" 전용.

## 배경지식 (자세한 내용은 `docs/plan.md`, `docs/updates.md`)

탐색기 자체의 "크기" 컬럼에 통합하는 셸 확장(`IColumnProvider`)을 먼저 시도했으나
Windows XP까지만 지원되는 죽은 API였음을 확인하고 독립 실행형 프로그램으로 전환했다.
