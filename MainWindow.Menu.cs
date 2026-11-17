// Copyright (c) dendr000. MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FolderSizeViewer
{
    // 상단 메뉴 바와 단축키. 메뉴 항목은 버튼이 부르는 것과 같은 함수를 부른다.
    public partial class MainWindow
    {
        private const int MenuListLimit = 10;
        private const int MenuExistCheckMillis = 300;
        private MenuItem _viewUpItem;
        private MenuItem _fileOpenInExplorerItem;

        private MenuItem NewMenuItem(string header, string gesture, Action action)
        {
            var item = new MenuItem { Header = header, InputGestureText = gesture, Style = _menuItemStyle };
            if (action == null) item.IsEnabled = false;
            else item.Click += (s, e) => action();
            return item;
        }

        private MenuItem NewTopMenu(string header)
        {
            return new MenuItem { Header = header, Style = _menuItemStyle };
        }

        private UIElement BuildMenuBar()
        {
            var menu = new Menu
            {
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = _theme.TextPrimary,
                Padding = new Thickness(6, 2, 6, 2),
                ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(StackPanel)))
            };
            menu.ItemsPanel.VisualTree.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            // 파일
            var file = NewTopMenu("파일");
            file.Items.Add(NewMenuItem("폴더 열기...", "Ctrl+O", BrowseForFolder));
            _fileOpenInExplorerItem = NewMenuItem("현재 폴더를 탐색기에서 열기", null, OpenCurrentInExplorer);
            file.Items.Add(_fileOpenInExplorerItem);
            file.Items.Add(NewMenuItem("새로고침", "F5", () => NavigateTo(_currentPath)));
            file.Items.Add(new Separator { Style = _separatorStyle });
            file.Items.Add(NewMenuItem("종료", "Alt+F4", Close));
            file.SubmenuOpened += (s, e) => _fileOpenInExplorerItem.IsEnabled = !string.IsNullOrEmpty(_currentPath);

            // 보기
            var view = NewTopMenu("보기");
            _viewUpItem = NewMenuItem("위 폴더로", "Alt+위", NavigateUp);
            view.Items.Add(_viewUpItem);
            view.Items.Add(NewMenuItem("드라이브 목록", null, () => NavigateTo(null)));
            view.Items.Add(new Separator { Style = _separatorStyle });
            view.Items.Add(NewMenuItem("라이트/다크 전환", "Ctrl+T", ToggleTheme));
            view.SubmenuOpened += (s, e) => _viewUpItem.IsEnabled = !string.IsNullOrEmpty(_currentPath);

            // 삭제 가능 폴더 바로가기 (즐겨찾기처럼 쓴다)
            var deletable = NewTopMenu("삭제 가능");
            deletable.SubmenuOpened += (s, e) => FillDeletableMenu(deletable);
            deletable.Items.Add(NewMenuItem("전체 목록 보기 / 검색...", "Ctrl+D", ShowDeletableList));

            // 설정
            var settings = NewTopMenu("설정");
            settings.Items.Add(NewMenuItem("표시·크기 기록 폴더 열기", null, OpenDataFolder));
            settings.Items.Add(NewMenuItem("저장된 폴더 크기 지우기...", null, ClearSizeCache));

            // 도움말
            var help = NewTopMenu("도움말");
            help.Items.Add(NewMenuItem("단축키", null, ShowShortcuts));
            help.Items.Add(NewMenuItem("폴더 크기 보기 정보", null, ShowAbout));

            menu.Items.Add(file);
            menu.Items.Add(view);
            menu.Items.Add(deletable);
            menu.Items.Add(settings);
            menu.Items.Add(help);

            var border = new Border
            {
                Background = _theme.ToolbarBackground,
                BorderBrush = _theme.Border,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = menu
            };
            return border;
        }

        // ---------- 삭제 가능 폴더 목록 ----------

        private void FillDeletableMenu(MenuItem menu)
        {
            // 표시가 많아져도 메뉴가 길어지지 않도록 기억된 크기가 큰 순서로 상위 몇 개만 보여 주고,
            // 전부는 별도 목록 창(검색 가능)에서 본다. 메뉴를 열 때 느려지지 않게 폴더 존재 확인은 보이는 항목만 한다.
            menu.Items.Clear();
            var paths = _marks.All.ToList();
            if (paths.Count == 0)
            {
                menu.Items.Add(NewMenuItem("표시한 폴더가 없습니다 (폴더를 우클릭해 표시)", null, null));
                return;
            }

            var ranked = paths
                .Select(p =>
                {
                    SizeCache.Entry entry;
                    bool known = _sizeCache.TryGet(p, out entry);
                    return new { Path = p, Known = known, Size = known ? entry.SizeBytes : -1L };
                })
                .OrderByDescending(x => x.Size)
                .ThenBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // 지워진 폴더의 옛 크기가 자리를 차지하지 않도록, 지금 있는 폴더만 큰 순서로 고른다.
            var shown = new List<MenuItem>();
            var clock = System.Diagnostics.Stopwatch.StartNew();
            foreach (var x in ranked)
            {
                // 연결 안 된 네트워크 경로 등으로 메뉴가 멈추지 않도록 확인에 쓰는 시간을 제한한다.
                if (shown.Count >= MenuListLimit || clock.ElapsedMilliseconds > MenuExistCheckMillis) break;
                string path = x.Path;
                bool exists;
                try { exists = Directory.Exists(path); } catch { exists = false; }
                if (!exists) continue;
                shown.Add(NewMenuItem(path, x.Known ? FormatSize(x.Size) : "크기 미확인", () => NavigateTo(path)));
            }

            menu.Items.Add(NewMenuItem(
                string.Format("표시한 폴더 {0}개 - 지금 있는 것 중 큰 순서 {1}개", paths.Count, shown.Count),
                null, null));
            menu.Items.Add(new Separator { Style = _separatorStyle });

            foreach (var item in shown) menu.Items.Add(item);
            if (shown.Count == 0)
                menu.Items.Add(NewMenuItem("지금 있는 표시 폴더가 없습니다 (없는 폴더는 전체 목록에서 확인)", null, null));

            menu.Items.Add(new Separator { Style = _separatorStyle });
            menu.Items.Add(NewMenuItem("전체 목록 보기 / 검색...", "Ctrl+D", ShowDeletableList));
        }

        // ---------- 파일·설정·도움말 동작 ----------

        private void OpenCurrentInExplorer()
        {
            if (string.IsNullOrEmpty(_currentPath)) return;
            try { System.Diagnostics.Process.Start("explorer.exe", "\"" + _currentPath + "\""); } catch { }
        }

        private void OpenDataFolder()
        {
            try
            {
                string dir = Path.GetDirectoryName(DeletableMarks.DefaultFilePath());
                if (string.IsNullOrEmpty(dir)) return;
                Directory.CreateDirectory(dir);
                System.Diagnostics.Process.Start("explorer.exe", "\"" + dir + "\"");
            }
            catch (Exception ex) { FlashStatus("폴더를 열지 못했습니다: " + ex.Message); }
        }

        private void ClearSizeCache()
        {
            var answer = MessageBox.Show(this,
                "저장해 둔 폴더 크기를 모두 지웁니다.\n다음에 폴더를 열 때 처음부터 다시 계산합니다.\n(\"삭제 가능\" 표시는 지워지지 않습니다.)",
                "저장된 폴더 크기 지우기", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (answer != MessageBoxResult.OK) return;

            _sizeCache.Clear();
            _sizeCacheBroken = false;
            SaveSizeCache();
            FlashStatus("저장된 폴더 크기를 지웠습니다.");
        }

        private void ShowShortcuts()
        {
            MessageBox.Show(this,
                "Ctrl+O\t폴더 열기\n" +
                "F5\t새로고침\n" +
                "Alt+위 화살표\t위 폴더로\n" +
                "Ctrl+T\t라이트/다크 전환\n" +
                "Ctrl+D\t삭제 가능 폴더 전체 목록\n" +
                "Enter (경로 입력칸)\t입력한 경로로 이동\n" +
                "더블클릭\t폴더 들어가기 / 파일 열기\n" +
                "우클릭\t탐색기에서 열기, 삭제 가능 표시",
                "단축키", MessageBoxButton.OK, MessageBoxImage.None);
        }

        private void ShowAbout()
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            MessageBox.Show(this,
                "폴더 크기 보기\n버전 " + v.ToString(3) + "\n\nCopyright (c) dendr000. MIT License.",
                "정보", MessageBoxButton.OK, MessageBoxImage.None);
        }

        // ---------- 단축키 ----------

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            ModifierKeys mods = Keyboard.Modifiers;

            if (mods == ModifierKeys.Control && key == Key.O) { BrowseForFolder(); e.Handled = true; }
            else if (mods == ModifierKeys.None && key == Key.F5) { NavigateTo(_currentPath); e.Handled = true; }
            else if (mods == ModifierKeys.Alt && key == Key.Up) { NavigateUp(); e.Handled = true; }
            else if (mods == ModifierKeys.Control && key == Key.T) { ToggleTheme(); e.Handled = true; }
            else if (mods == ModifierKeys.Control && key == Key.D) { ShowDeletableList(); e.Handled = true; }

            base.OnPreviewKeyDown(e);
        }
    }
}
