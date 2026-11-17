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
            FillDeletableMenu(deletable);

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
            menu.Items.Clear();
            var paths = _marks.All.ToList();
            if (paths.Count == 0)
            {
                menu.Items.Add(NewMenuItem("표시한 폴더가 없습니다 (폴더를 우클릭해 표시)", null, null));
                return;
            }

            long total = 0;
            int existing = 0;
            foreach (string p in paths)
            {
                if (!Directory.Exists(p)) continue;
                existing++;
                SizeCache.Entry entry;
                if (!_marks.IsInherited(p) && _sizeCache.TryGet(p, out entry)) total += entry.SizeBytes;
            }
            menu.Items.Add(NewMenuItem(
                string.Format("표시한 폴더 {0}개 (지금 있는 것 {1}개)", paths.Count, existing),
                "기억된 크기 합계 " + FormatSize(total), null));
            menu.Items.Add(new Separator { Style = _separatorStyle });

            foreach (string p in paths)
            {
                string path = p;
                bool exists = Directory.Exists(path);
                SizeCache.Entry entry;
                string size;
                if (!exists) size = "없음";
                else if (_sizeCache.TryGet(path, out entry)) size = FormatSize(entry.SizeBytes);
                else size = "크기 미확인";
                menu.Items.Add(NewMenuItem(path, size, exists ? (Action)(() => NavigateTo(path)) : null));
            }
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

            base.OnPreviewKeyDown(e);
        }
    }
}
