// Copyright (c) dendr000. MIT License.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FolderSizeViewer
{
    // "삭제 가능" 폴더 전체 목록 창. 표시가 수백 개로 늘어도 검색해서 바로 찾도록 한다.
    public partial class MainWindow
    {
        private Window _favWindow;
        private ListView _favList;
        private TextBox _favFilter;
        private TextBlock _favSummary;
        private readonly ObservableCollection<RowItem> _favItems = new ObservableCollection<RowItem>();
        private List<RowItem> _favAll = new List<RowItem>();
        private long _favTotal;
        private int _favExisting;
        private int _favLoadId;

        private const string FavMissing = "없음";

        private struct FavSnapshot
        {
            public string Path;
            public long Size;      // 기억된 크기, 없으면 -1
            public bool Inherited; // 상위 폴더가 이미 표시돼 있음
        }

        private void ShowDeletableList()
        {
            if (_favWindow != null)
            {
                _favWindow.Activate();
                ReloadDeletableList();
                return;
            }

            _favWindow = BuildDeletableListWindow();
            _favWindow.Closed += (s, e) => { _favWindow = null; _favLoadId++; };
            _favWindow.Show();
            ReloadDeletableList();
            _favFilter.Focus();
        }

        private Window BuildDeletableListWindow()
        {
            var win = new Window
            {
                Title = "삭제 가능 폴더",
                Owner = this,
                Width = 960,
                Height = 540,
                MinWidth = 560,
                MinHeight = 320,
                Background = _theme.WindowBackground,
                FontFamily = FontFamily,
                Icon = Icon,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var filterBorder = new Border
            {
                Background = _theme.ControlBackground,
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(12, 12, 12, 0)
            };
            _favFilter = new TextBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = _theme.TextPrimary,
                CaretBrush = _theme.TextPrimary,
                Padding = new Thickness(12, 8, 12, 8),
                FontSize = 13,
                ToolTip = "경로의 일부를 입력하면 걸러서 보여 줍니다"
            };
            _favFilter.TextChanged += (s, e) => ApplyFavFilter();
            filterBorder.Child = _favFilter;
            Grid.SetRow(filterBorder, 0);

            _favList = new ListView
            {
                ItemsSource = _favItems,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                AlternationCount = 2,
                Margin = new Thickness(4)
            };
            var gridView = new GridView { ColumnHeaderContainerStyle = BuildHeaderStyle() };
            var pathColumn = new GridViewColumn { Header = "경로", Width = 560, CellTemplate = BuildTextTemplate("Name", _theme.TextPrimary) };
            gridView.Columns.Add(pathColumn);
            gridView.Columns.Add(new GridViewColumn { Header = "상태", Width = 90, CellTemplate = BuildTextTemplate("TypeLabel", _theme.TextSecondary) });
            gridView.Columns.Add(new GridViewColumn { Header = "기억된 크기", Width = 140, CellTemplate = BuildTextTemplate("SizeText", _theme.TextPrimary) });
            _favList.View = gridView;
            _favList.ItemContainerStyle = BuildRowStyle();
            _favList.MouseDoubleClick += (s, e) => FavGo();
            ScrollViewer.SetVerticalScrollBarVisibility(_favList, ScrollBarVisibility.Auto);

            var listBorder = new Border
            {
                Background = _theme.SurfaceBackground,
                Margin = new Thickness(12, 10, 12, 10),
                CornerRadius = new CornerRadius(10),
                Child = _favList
            };
            Grid.SetRow(listBorder, 1);

            var bottom = new Grid { Margin = new Thickness(12, 0, 12, 12) };
            bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _favSummary = new TextBlock
            {
                Foreground = _theme.TextSecondary,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 0, 12, 0)
            };
            Grid.SetColumn(_favSummary, 0);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal };
            buttons.Children.Add(CreatePlainButton("이동", FavGo));
            buttons.Children.Add(CreatePlainButton("탐색기에서 열기", FavOpenInExplorer));
            buttons.Children.Add(CreatePlainButton("표시 해제", FavUnmark));
            Grid.SetColumn(buttons, 1);

            bottom.Children.Add(_favSummary);
            bottom.Children.Add(buttons);
            Grid.SetRow(bottom, 2);

            root.Children.Add(filterBorder);
            root.Children.Add(listBorder);
            root.Children.Add(bottom);
            win.Content = root;

            // 경로 칸은 창 폭에 맞춰 늘려, 깊은 경로의 끝(폴더 이름)이 잘리지 않게 한다.
            win.SizeChanged += (s, e) => pathColumn.Width = Math.Max(240, win.ActualWidth - 330);

            win.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) { win.Close(); e.Handled = true; }
                else if (e.Key == Key.Enter && !_favFilter.IsKeyboardFocused) { FavGo(); e.Handled = true; }
            };
            return win;
        }

        private Button CreatePlainButton(string text, Action action)
        {
            var btn = new Button
            {
                Content = new TextBlock { Text = text, Foreground = _theme.TextPrimary, FontSize = 13, TextWrapping = TextWrapping.NoWrap },
                Height = 34,
                Padding = new Thickness(14, 0, 14, 0),
                Margin = new Thickness(8, 0, 0, 0),
                Cursor = Cursors.Hand,
                Background = _theme.ControlBackground,
                BorderThickness = new Thickness(0)
            };
            ApplyFlatButtonTemplate(btn, new CornerRadius(8));
            btn.Click += (s, e) => action();
            return btn;
        }

        // 기록과 기억된 크기는 화면 스레드에서 읽고, 느릴 수 있는 폴더 존재 확인만 뒤에서 한다.
        private void ReloadDeletableList()
        {
            if (_favWindow == null) return;
            int id = ++_favLoadId;
            _favSummary.Text = "불러오는 중...";

            var snapshots = new List<FavSnapshot>();
            foreach (string p in _marks.All)
            {
                SizeCache.Entry entry;
                snapshots.Add(new FavSnapshot
                {
                    Path = p,
                    Size = _sizeCache.TryGet(p, out entry) ? entry.SizeBytes : -1,
                    Inherited = _marks.IsInherited(p)
                });
            }

            Task.Run(() =>
            {
                var rows = new List<RowItem>();
                long total = 0;
                int existing = 0;
                foreach (var s in snapshots)
                {
                    bool exists;
                    try { exists = Directory.Exists(s.Path); } catch { exists = false; }
                    if (exists)
                    {
                        existing++;
                        if (!s.Inherited && s.Size >= 0) total += s.Size;
                    }
                    rows.Add(new RowItem
                    {
                        FullPath = s.Path,
                        IsDirectory = true,
                        Name = s.Path,
                        TypeLabel = !exists ? FavMissing : (s.Inherited ? "상위 표시" : "있음"),
                        SizeBytes = exists && s.Size >= 0 ? s.Size : -1,
                        SizeText = !exists ? "-" : (s.Size >= 0 ? FormatSize(s.Size) : "크기 미확인")
                    });
                }

                var sorted = rows
                    .OrderBy(r => r.TypeLabel == FavMissing ? 1 : 0)
                    .ThenByDescending(r => r.SizeBytes)
                    .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                try
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        if (id != _favLoadId || _favWindow == null) return;
                        _favAll = sorted;
                        _favTotal = total;
                        _favExisting = existing;
                        ApplyFavFilter();
                    }));
                }
                catch { }
            });
        }

        private void ApplyFavFilter()
        {
            if (_favSummary == null || _favFilter == null) return;
            string q = _favFilter.Text.Trim();

            _favItems.Clear();
            foreach (var r in _favAll)
                if (q.Length == 0 || r.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    _favItems.Add(r);

            _favSummary.Text = string.Format("{0}개 표시 / 전체 {1}개 (지금 있는 것 {2}개) - 기억된 크기 합계 {3}",
                _favItems.Count, _favAll.Count, _favExisting, FormatSize(_favTotal));
        }

        private RowItem FavSelected()
        {
            return _favList == null ? null : _favList.SelectedItem as RowItem;
        }

        private void FavGo()
        {
            var row = FavSelected();
            if (row == null || row.TypeLabel == FavMissing) return;
            NavigateTo(row.FullPath);
            Activate();
        }

        private void FavOpenInExplorer()
        {
            var row = FavSelected();
            if (row == null || row.TypeLabel == FavMissing) return;
            try { System.Diagnostics.Process.Start("explorer.exe", "\"" + row.FullPath + "\""); } catch { }
        }

        private void FavUnmark()
        {
            var row = FavSelected();
            if (row == null) return;
            _marks.Remove(row.FullPath);
            try { _marks.Save(); }
            catch (Exception ex) { FlashStatus("표시를 저장하지 못했습니다: " + ex.Message); }
            RefreshRowMarks();
        }
    }
}
