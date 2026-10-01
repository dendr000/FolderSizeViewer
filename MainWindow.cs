// Copyright (c) dendr000. MIT License.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace FolderSizeViewer
{
    public class MainWindow : Window
    {
        private readonly Theme _theme;
        private readonly ObservableCollection<RowItem> _items = new ObservableCollection<RowItem>();

        private TextBox _pathBox;
        private Button _upButton;
        private ListView _listView;
        private GridViewColumn _colName, _colType, _colSize;
        private TextBlock _statusText;
        private FrameworkElement _spinner;
        private ContentControl _themeIconHost;
        private Border _contentBorder;

        private string _currentPath;
        private CancellationTokenSource _scanCts;
        private string _sortProperty = "SizeBytes";
        private bool _sortAscending = false;
        private bool _isScanning;

        private static readonly Geometry FolderGeometry = Icons.FolderGeometry();
        private static readonly Geometry FileGeometry = Icons.FileGeometry();

        public MainWindow(string startPath)
        {
            _theme = new Theme(Theme.IsSystemInDarkMode());

            Title = "폴더 크기 보기";
            Width = 980;
            Height = 640;
            MinWidth = 720;
            MinHeight = 420;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = _theme.WindowBackground;
            FontFamily = new FontFamily("Segoe UI");

            BuildUi();

            NavigateTo(!string.IsNullOrEmpty(startPath) && Directory.Exists(startPath) ? startPath : null);
        }

        // ---------- UI construction ----------

        private void BuildUi()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var toolbar = BuildToolbar();
            Grid.SetRow(toolbar, 0);

            _contentBorder = new Border { Background = _theme.SurfaceBackground, Margin = new Thickness(12, 10, 12, 10), CornerRadius = new CornerRadius(10) };
            _contentBorder.Child = BuildListView();
            Grid.SetRow(_contentBorder, 1);

            var statusBar = BuildStatusBar();
            Grid.SetRow(statusBar, 2);

            root.Children.Add(toolbar);
            root.Children.Add(_contentBorder);
            root.Children.Add(statusBar);

            Content = root;
        }

        private UIElement BuildToolbar()
        {
            var border = new Border
            {
                Background = _theme.ToolbarBackground,
                BorderBrush = _theme.Border,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(12, 10, 12, 10)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _upButton = CreateIconButton(Icons.ArrowUp(_theme.TextPrimary), "위 폴더로");
            _upButton.Click += (s, e) => NavigateUp();
            Grid.SetColumn(_upButton, 0);

            var pathBorder = new Border
            {
                Background = _theme.ControlBackground,
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(8, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Stretch
            };
            _pathBox = new TextBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = _theme.TextPrimary,
                CaretBrush = _theme.TextPrimary,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(12, 8, 12, 8),
                FontSize = 13
            };
            _pathBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    string p = _pathBox.Text.Trim();
                    if (string.IsNullOrEmpty(p)) NavigateTo(null);
                    else if (Directory.Exists(p)) NavigateTo(p);
                    else FlashStatus("경로를 찾을 수 없습니다: " + p);
                }
            };
            pathBorder.Child = _pathBox;
            Grid.SetColumn(pathBorder, 1);

            var browseButton = CreateTextButton(Icons.Search(_theme.TextPrimary), "찾아보기");
            browseButton.Margin = new Thickness(0, 0, 8, 0);
            browseButton.Click += (s, e) => BrowseForFolder();
            Grid.SetColumn(browseButton, 2);

            var refreshButton = CreateTextButton(Icons.Refresh(_theme.TextPrimary), "새로고침");
            refreshButton.Margin = new Thickness(0, 0, 8, 0);
            refreshButton.Click += (s, e) => NavigateTo(_currentPath);
            Grid.SetColumn(refreshButton, 3);

            _themeIconHost = new ContentControl
            {
                Content = _theme.IsDark ? Icons.Moon(_theme.TextPrimary) : Icons.Sun(_theme.TextPrimary),
                Width = 20,
                Height = 20
            };
            var themeButton = CreateIconButton(_themeIconHost, "테마 전환 (라이트/다크)");
            themeButton.Click += (s, e) => ToggleTheme();
            Grid.SetColumn(themeButton, 4);

            grid.Children.Add(_upButton);
            grid.Children.Add(pathBorder);
            grid.Children.Add(browseButton);
            grid.Children.Add(refreshButton);
            grid.Children.Add(themeButton);

            border.Child = grid;
            return border;
        }

        private Button CreateIconButton(object content, string tooltip)
        {
            var btn = new Button
            {
                Content = content,
                Width = 38,
                Height = 38,
                ToolTip = tooltip,
                Cursor = Cursors.Hand,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };
            ApplyFlatButtonTemplate(btn, new CornerRadius(8));
            return btn;
        }

        private Button CreateTextButton(UIElement icon, string text)
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            icon.SetValue(FrameworkElement.WidthProperty, 15.0);
            icon.SetValue(FrameworkElement.HeightProperty, 15.0);
            icon.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            icon.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 6, 0));
            stack.Children.Add(icon);
            stack.Children.Add(new TextBlock { Text = text, Foreground = _theme.TextPrimary, FontSize = 13, VerticalAlignment = VerticalAlignment.Center });

            var btn = new Button
            {
                Content = stack,
                Height = 38,
                Padding = new Thickness(14, 0, 14, 0),
                Cursor = Cursors.Hand,
                Background = _theme.ControlBackground,
                BorderThickness = new Thickness(0)
            };
            ApplyFlatButtonTemplate(btn, new CornerRadius(8));
            return btn;
        }

        private void ApplyFlatButtonTemplate(Button btn, CornerRadius radius)
        {
            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, radius);
            var presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(presenterFactory);
            template.VisualTree = borderFactory;

            var hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Button.BackgroundProperty, _theme.ControlHover));
            template.Triggers.Add(hoverTrigger);

            btn.Template = template;
        }

        private ListView BuildListView()
        {
            _listView = new ListView
            {
                ItemsSource = _items,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                AlternationCount = 2,
                Margin = new Thickness(4)
            };

            var gridView = new GridView { ColumnHeaderContainerStyle = BuildHeaderStyle() };

            _colName = new GridViewColumn { Header = "이름", Width = 400, CellTemplate = BuildNameTemplate() };
            _colType = new GridViewColumn { Header = "종류", Width = 140, CellTemplate = BuildTextTemplate("TypeLabel", _theme.TextSecondary) };
            _colSize = new GridViewColumn { Header = "디스크 사용량", Width = 160, CellTemplate = BuildSizeTemplate() };

            gridView.Columns.Add(_colName);
            gridView.Columns.Add(_colType);
            gridView.Columns.Add(_colSize);
            _listView.View = gridView;

            _listView.ItemContainerStyle = BuildRowStyle();
            _listView.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(OnHeaderClick));
            _listView.MouseDoubleClick += (s, e) =>
            {
                var row = _listView.SelectedItem as RowItem;
                if (row == null) return;
                if (row.IsDirectory) NavigateTo(row.FullPath);
                else { try { System.Diagnostics.Process.Start(row.FullPath); } catch { } }
            };

            var openMenu = new MenuItem { Header = "탐색기에서 열기" };
            openMenu.Click += (s, e) =>
            {
                var row = _listView.SelectedItem as RowItem;
                if (row == null) return;
                try { System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + row.FullPath + "\""); } catch { }
            };
            _listView.ContextMenu = new ContextMenu();
            _listView.ContextMenu.Items.Add(openMenu);

            ScrollViewer.SetVerticalScrollBarVisibility(_listView, ScrollBarVisibility.Auto);
            return _listView;
        }

        private Style BuildHeaderStyle()
        {
            var style = new Style(typeof(GridViewColumnHeader));
            var template = new ControlTemplate(typeof(GridViewColumnHeader));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, _theme.SurfaceBackground);
            borderFactory.SetValue(Border.BorderBrushProperty, _theme.Border);
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(0, 0, 0, 1));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(10, 8, 10, 8));
            var presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(TextBlock.ForegroundProperty, _theme.TextSecondary);
            presenterFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            presenterFactory.SetValue(TextBlock.FontSizeProperty, 12.0);
            borderFactory.AppendChild(presenterFactory);
            template.VisualTree = borderFactory;
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            style.Setters.Add(new Setter(Control.CursorProperty, Cursors.Hand));
            return style;
        }

        private Style BuildRowStyle()
        {
            var style = new Style(typeof(ListViewItem));
            var template = new ControlTemplate(typeof(ListViewItem));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "RowBorder";
            borderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            borderFactory.SetValue(Border.MarginProperty, new Thickness(2, 1, 2, 1));
            var presenterFactory = new FrameworkElementFactory(typeof(GridViewRowPresenter));
            presenterFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(6, 4, 6, 4));
            borderFactory.AppendChild(presenterFactory);
            template.VisualTree = borderFactory;

            var altTrigger = new Trigger { Property = ItemsControl.AlternationIndexProperty, Value = 1 };
            altTrigger.Setters.Add(new Setter(Border.BackgroundProperty, _theme.RowAlternate) { TargetName = "RowBorder" });
            template.Triggers.Add(altTrigger);

            var hoverTrigger = new Trigger { Property = ListViewItem.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, _theme.RowHover) { TargetName = "RowBorder" });
            template.Triggers.Add(hoverTrigger);

            var selectedTrigger = new Trigger { Property = ListViewItem.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, _theme.RowSelected) { TargetName = "RowBorder" });
            template.Triggers.Add(selectedTrigger);

            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            style.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Hand));
            return style;
        }

        private DataTemplate BuildNameTemplate()
        {
            var template = new DataTemplate();
            var stack = new FrameworkElementFactory(typeof(StackPanel));
            stack.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            var folderIcon = new FrameworkElementFactory(typeof(System.Windows.Shapes.Path));
            folderIcon.SetValue(System.Windows.Shapes.Path.DataProperty, FolderGeometry);
            folderIcon.SetValue(Shape.FillProperty, _theme.Accent);
            folderIcon.SetValue(FrameworkElement.WidthProperty, 16.0);
            folderIcon.SetValue(FrameworkElement.HeightProperty, 16.0);
            folderIcon.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));
            folderIcon.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            folderIcon.SetBinding(UIElement.VisibilityProperty, new Binding("IsDirectory") { Converter = new BoolToVisibilityConverter() });

            var fileIcon = new FrameworkElementFactory(typeof(System.Windows.Shapes.Path));
            fileIcon.SetValue(System.Windows.Shapes.Path.DataProperty, FileGeometry);
            fileIcon.SetValue(Shape.FillProperty, _theme.TextSecondary);
            fileIcon.SetValue(FrameworkElement.WidthProperty, 16.0);
            fileIcon.SetValue(FrameworkElement.HeightProperty, 16.0);
            fileIcon.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));
            fileIcon.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            fileIcon.SetBinding(UIElement.VisibilityProperty, new Binding("IsDirectory") { Converter = new BoolToVisibilityConverter { Invert = true } });

            var text = new FrameworkElementFactory(typeof(TextBlock));
            text.SetValue(TextBlock.ForegroundProperty, _theme.TextPrimary);
            text.SetValue(TextBlock.FontSizeProperty, 13.0);
            text.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            text.SetBinding(TextBlock.TextProperty, new Binding("Name"));

            stack.AppendChild(folderIcon);
            stack.AppendChild(fileIcon);
            stack.AppendChild(text);
            template.VisualTree = stack;
            return template;
        }

        private DataTemplate BuildTextTemplate(string bindingPath, Brush foreground)
        {
            var template = new DataTemplate();
            var text = new FrameworkElementFactory(typeof(TextBlock));
            text.SetValue(TextBlock.ForegroundProperty, foreground);
            text.SetValue(TextBlock.FontSizeProperty, 13.0);
            text.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            text.SetBinding(TextBlock.TextProperty, new Binding(bindingPath));
            template.VisualTree = text;
            return template;
        }

        private DataTemplate BuildSizeTemplate()
        {
            var template = new DataTemplate();
            var stack = new FrameworkElementFactory(typeof(StackPanel));
            stack.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 2, 0, 2));

            var text = new FrameworkElementFactory(typeof(TextBlock));
            text.SetValue(TextBlock.ForegroundProperty, _theme.TextPrimary);
            text.SetValue(TextBlock.FontSizeProperty, 13.0);
            text.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Right);
            text.SetBinding(TextBlock.TextProperty, new Binding("SizeText"));

            var track = new FrameworkElementFactory(typeof(Border));
            track.SetValue(Border.BackgroundProperty, _theme.ControlBackground);
            track.SetValue(Border.CornerRadiusProperty, new CornerRadius(2));
            track.SetValue(FrameworkElement.HeightProperty, 4.0);
            track.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 5, 0, 0));
            track.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            track.SetValue(FrameworkElement.WidthProperty, 90.0);
            track.SetBinding(UIElement.VisibilityProperty, new Binding("IsCalculating") { Converter = new BoolToVisibilityConverter { Invert = true } });

            var fill = new FrameworkElementFactory(typeof(Border));
            fill.SetValue(Border.BackgroundProperty, _theme.Accent);
            fill.SetValue(Border.CornerRadiusProperty, new CornerRadius(2));
            fill.SetValue(FrameworkElement.HeightProperty, 4.0);
            fill.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            fill.SetBinding(FrameworkElement.WidthProperty, new Binding("BarFraction") { Converter = new FractionToWidthConverter { MaxWidth = 90 } });
            track.AppendChild(fill);

            stack.AppendChild(text);
            stack.AppendChild(track);
            template.VisualTree = stack;
            return template;
        }

        private UIElement BuildStatusBar()
        {
            var border = new Border
            {
                Background = _theme.ToolbarBackground,
                BorderBrush = _theme.Border,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(14, 6, 14, 6)
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _statusText = new TextBlock { Foreground = _theme.TextSecondary, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(_statusText, 0);

            _spinner = Icons.Refresh(_theme.Accent);
            _spinner.Width = 14;
            _spinner.Height = 14;
            _spinner.Visibility = Visibility.Collapsed;
            _spinner.RenderTransformOrigin = new Point(0.5, 0.5);
            _spinner.RenderTransform = new RotateTransform(0);
            Grid.SetColumn(_spinner, 1);

            grid.Children.Add(_statusText);
            grid.Children.Add(_spinner);
            border.Child = grid;
            return border;
        }

        // ---------- Theme ----------

        private void ToggleTheme()
        {
            bool goingDark = !_theme.IsDark;
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(110));
            fadeOut.Completed += (s, e) =>
            {
                _themeIconHost.Content = goingDark ? Icons.Moon(_theme.TextPrimary) : Icons.Sun(_theme.TextPrimary);
                _themeIconHost.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));
            };
            _themeIconHost.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            _theme.SwitchTo(goingDark, TimeSpan.FromMilliseconds(260));
        }

        private void SetScanning(bool scanning)
        {
            _isScanning = scanning;
            _spinner.Visibility = scanning ? Visibility.Visible : Visibility.Collapsed;
            if (scanning)
            {
                var anim = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(1)) { RepeatBehavior = RepeatBehavior.Forever };
                ((RotateTransform)_spinner.RenderTransform).BeginAnimation(RotateTransform.AngleProperty, anim);
            }
            else
            {
                ((RotateTransform)_spinner.RenderTransform).BeginAnimation(RotateTransform.AngleProperty, null);
            }
        }

        private void FlashStatus(string message)
        {
            _statusText.Text = message;
        }

        // ---------- Navigation & scanning ----------

        private void BrowseForFolder()
        {
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (!string.IsNullOrEmpty(_currentPath)) dlg.SelectedPath = _currentPath;
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    NavigateTo(dlg.SelectedPath);
            }
        }

        private void NavigateUp()
        {
            if (string.IsNullOrEmpty(_currentPath)) return;
            var parent = Directory.GetParent(_currentPath);
            NavigateTo(parent == null ? null : parent.FullName);
        }

        private void OnHeaderClick(object sender, RoutedEventArgs e)
        {
            var header = e.OriginalSource as GridViewColumnHeader;
            if (header == null || header.Column == null) return;

            string prop;
            if (header.Column == _colName) prop = "Name";
            else if (header.Column == _colType) prop = "TypeLabel";
            else prop = "SizeBytes";

            if (_sortProperty == prop) _sortAscending = !_sortAscending;
            else { _sortProperty = prop; _sortAscending = true; }
            ApplySort();
        }

        private void ApplySort()
        {
            var view = CollectionViewSource.GetDefaultView(_items);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(_sortProperty, _sortAscending ? ListSortDirection.Ascending : ListSortDirection.Descending));
        }

        private void NavigateTo(string path)
        {
            if (_scanCts != null) _scanCts.Cancel();
            _scanCts = new CancellationTokenSource();
            var token = _scanCts.Token;

            _currentPath = path;
            _pathBox.Text = path ?? "";
            _upButton.IsEnabled = !string.IsNullOrEmpty(path);
            _items.Clear();
            _statusText.Text = "불러오는 중...";

            if (string.IsNullOrEmpty(path))
            {
                LoadDriveList();
                PlayContentFadeIn();
                return;
            }

            var dirRows = new List<RowItem>();
            try
            {
                foreach (var dir in SafeEnumerateDirectories(path))
                {
                    var row = new RowItem
                    {
                        FullPath = dir,
                        IsDirectory = true,
                        Name = System.IO.Path.GetFileName(dir),
                        TypeLabel = "폴더",
                        SizeText = "계산 중...",
                        IsCalculating = true
                    };
                    _items.Add(row);
                    dirRows.Add(row);
                }

                foreach (var file in SafeEnumerateFiles(path))
                {
                    long len = 0;
                    try { len = DiskSize.GetFileSizeOnDisk(file); } catch { }
                    var row = new RowItem
                    {
                        FullPath = file,
                        IsDirectory = false,
                        Name = System.IO.Path.GetFileName(file),
                        TypeLabel = GetFileTypeLabel(file),
                        SizeBytes = len,
                        SizeText = FormatSize(len),
                        IsCalculating = false
                    };
                    _items.Add(row);
                }
            }
            catch (Exception ex)
            {
                _statusText.Text = "오류: " + ex.Message;
                return;
            }

            RecomputeBarFractions();
            ApplySort();
            PlayContentFadeIn();
            _statusText.Text = _items.Count + "개 항목" + (dirRows.Count > 0 ? " - 폴더 크기 계산 중..." : "");

            if (dirRows.Count > 0) ScanFolderSizesAsync(dirRows, token);
        }

        private void PlayContentFadeIn()
        {
            _contentBorder.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));
        }

        private void RecomputeBarFractions()
        {
            long max = 0;
            foreach (var r in _items) if (r.SizeBytes > max) max = r.SizeBytes;
            foreach (var r in _items) r.BarFraction = max > 0 ? (double)r.SizeBytes / max : 0;
        }

        private void ScanFolderSizesAsync(List<RowItem> dirRows, CancellationToken token)
        {
            SetScanning(true);
            Task.Run(() =>
            {
                int done = 0;
                foreach (var row in dirRows)
                {
                    if (token.IsCancellationRequested) return;
                    long size = ComputeDirectorySize(row.FullPath, 0, token);
                    if (token.IsCancellationRequested) return;
                    done++;
                    int doneCopy = done;

                    try
                    {
                        Dispatcher.Invoke((Action)(() =>
                        {
                            if (token.IsCancellationRequested) return;
                            row.SizeBytes = size;
                            row.SizeText = FormatSize(size);
                            row.IsCalculating = false;
                            RecomputeBarFractions();
                            _statusText.Text = string.Format("{0}개 항목 - 폴더 크기 계산 중 ({1}/{2})", _items.Count, doneCopy, dirRows.Count);
                        }));
                    }
                    catch { return; }
                }

                try
                {
                    Dispatcher.Invoke((Action)(() =>
                    {
                        if (!token.IsCancellationRequested)
                        {
                            _statusText.Text = _items.Count + "개 항목";
                            SetScanning(false);
                        }
                    }));
                }
                catch { }
            }, token);
        }

        private void LoadDriveList()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;
                long used;
                try { used = drive.TotalSize - drive.AvailableFreeSpace; }
                catch { continue; }

                string label = drive.VolumeLabel;
                string name = string.IsNullOrEmpty(label)
                    ? string.Format("로컬 디스크 ({0})", drive.Name.TrimEnd('\\'))
                    : string.Format("{0} ({1})", label, drive.Name.TrimEnd('\\'));

                _items.Add(new RowItem
                {
                    FullPath = drive.RootDirectory.FullName,
                    IsDirectory = true,
                    Name = name,
                    TypeLabel = "드라이브",
                    SizeBytes = used,
                    SizeText = FormatSize(used),
                    IsCalculating = false
                });
            }

            RecomputeBarFractions();
            _sortProperty = "SizeBytes";
            _sortAscending = false;
            ApplySort();
            _statusText.Text = _items.Count + "개 드라이브";
        }

        private static IEnumerable<string> SafeEnumerateDirectories(string path)
        {
            try { return Directory.EnumerateDirectories(path).ToList(); }
            catch (UnauthorizedAccessException) { return Enumerable.Empty<string>(); }
            catch (IOException) { return Enumerable.Empty<string>(); }
        }

        private static IEnumerable<string> SafeEnumerateFiles(string path)
        {
            try { return Directory.EnumerateFiles(path).ToList(); }
            catch (UnauthorizedAccessException) { return Enumerable.Empty<string>(); }
            catch (IOException) { return Enumerable.Empty<string>(); }
        }

        private static long ComputeDirectorySize(string path, int depth, CancellationToken token)
        {
            if (depth > 64 || token.IsCancellationRequested) return 0;

            long total = 0;
            try
            {
                var dirInfo = new DirectoryInfo(path);
                if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                    return 0;

                foreach (var file in dirInfo.EnumerateFiles())
                {
                    if (token.IsCancellationRequested) return total;
                    try { total += DiskSize.GetFileSizeOnDisk(file.FullName); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }

                foreach (var dir in dirInfo.EnumerateDirectories())
                {
                    if (token.IsCancellationRequested) return total;
                    total += ComputeDirectorySize(dir.FullName, depth + 1, token);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }

            return total;
        }

        private static string GetFileTypeLabel(string path)
        {
            string ext = System.IO.Path.GetExtension(path);
            if (string.IsNullOrEmpty(ext)) return "파일";
            return ext.TrimStart('.').ToUpperInvariant() + " 파일";
        }

        private static string FormatSize(long bytes)
        {
            string[] units = { "바이트", "KB", "MB", "GB", "TB", "PB" };
            double value = bytes;
            int unitIndex = 0;
            while (value >= 1024 && unitIndex < units.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }
            return unitIndex == 0
                ? string.Format("{0:N0} {1}", value, units[unitIndex])
                : string.Format("{0:N2} {1}", value, units[unitIndex]);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_scanCts != null) _scanCts.Cancel();
            base.OnClosing(e);
        }
    }
}
