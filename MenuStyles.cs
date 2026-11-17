// Copyright (c) dendr000. MIT License.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace FolderSizeViewer
{
    /// <summary>
    /// 메뉴 바, 드롭다운, 우클릭 메뉴를 앱 테마 색으로 그리기 위한 모양 모음.
    /// 기본 모양은 어두운 테마에서도 흰 상자로 보이기 때문이다.
    /// </summary>
    public static class MenuStyles
    {
        public static Style MenuItemStyle(Theme t)
        {
            var template = new ControlTemplate(typeof(MenuItem));

            var grid = new FrameworkElementFactory(typeof(Grid));

            var root = new FrameworkElementFactory(typeof(Border));
            root.Name = "Root";
            root.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            root.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            root.SetValue(Border.PaddingProperty, new Thickness(12, 6, 12, 6));
            root.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

            var dock = new FrameworkElementFactory(typeof(DockPanel));
            dock.SetValue(TextElement.ForegroundProperty, new TemplateBindingExtension(Control.ForegroundProperty));

            var gesture = new FrameworkElementFactory(typeof(TextBlock));
            gesture.Name = "Gesture";
            gesture.SetValue(DockPanel.DockProperty, Dock.Right);
            gesture.SetValue(FrameworkElement.MarginProperty, new Thickness(28, 0, 0, 0));
            gesture.SetValue(TextBlock.ForegroundProperty, t.TextSecondary);
            gesture.SetValue(TextBlock.TextWrappingProperty, TextWrapping.NoWrap);
            gesture.SetBinding(TextBlock.TextProperty, new Binding("InputGestureText") { RelativeSource = RelativeSource.TemplatedParent });

            var header = new FrameworkElementFactory(typeof(ContentPresenter));
            header.SetValue(ContentPresenter.ContentSourceProperty, "Header");
            header.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
            header.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            dock.AppendChild(gesture);
            dock.AppendChild(header);
            root.AppendChild(dock);

            var popup = new FrameworkElementFactory(typeof(Popup));
            popup.Name = "Popup";
            popup.SetValue(Popup.PlacementProperty, PlacementMode.Right);
            popup.SetValue(Popup.AllowsTransparencyProperty, true);
            popup.SetValue(Popup.FocusableProperty, false);
            popup.SetValue(Popup.PopupAnimationProperty, PopupAnimation.Fade);
            popup.SetValue(Popup.CustomPopupPlacementCallbackProperty, new CustomPopupPlacementCallback(
                (popupSize, targetSize, offset) => new[]
                {
                    new CustomPopupPlacement(new Point(0, targetSize.Height), PopupPrimaryAxis.Horizontal)
                }));
            popup.SetBinding(Popup.IsOpenProperty, new Binding("IsSubmenuOpen") { RelativeSource = RelativeSource.TemplatedParent });

            var panel = new FrameworkElementFactory(typeof(Border));
            panel.SetValue(Border.BackgroundProperty, t.SurfaceBackground);
            panel.SetValue(Border.BorderBrushProperty, t.Border);
            panel.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            panel.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            panel.SetValue(Border.PaddingProperty, new Thickness(4));
            panel.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 6, 6));
            panel.SetValue(FrameworkElement.MinWidthProperty, 200.0);
            var items = new FrameworkElementFactory(typeof(ItemsPresenter));
            panel.AppendChild(items);
            popup.AppendChild(panel);

            grid.AppendChild(root);
            grid.AppendChild(popup);
            template.VisualTree = grid;

            var top = new Trigger { Property = MenuItem.RoleProperty, Value = MenuItemRole.TopLevelHeader };
            // 시스템의 "메뉴 오른쪽 정렬" 설정과 무관하게 항상 메뉴 왼쪽 끝에 맞춰 연다.
            top.Setters.Add(new Setter(Popup.PlacementProperty, PlacementMode.Custom) { TargetName = "Popup" });
            top.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0)) { TargetName = "Gesture" });
            top.Setters.Add(new Setter(Border.PaddingProperty, new Thickness(10, 5, 10, 5)) { TargetName = "Root" });
            template.Triggers.Add(top);

            var highlight = new Trigger { Property = MenuItem.IsHighlightedProperty, Value = true };
            highlight.Setters.Add(new Setter(Border.BackgroundProperty, t.ControlHover) { TargetName = "Root" });
            template.Triggers.Add(highlight);

            var open = new Trigger { Property = MenuItem.IsSubmenuOpenProperty, Value = true };
            open.Setters.Add(new Setter(Border.BackgroundProperty, t.ControlHover) { TargetName = "Root" });
            template.Triggers.Add(open);

            var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.4) { TargetName = "Root" });
            template.Triggers.Add(disabled);

            var style = new Style(typeof(MenuItem));
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            style.Setters.Add(new Setter(Control.ForegroundProperty, t.TextPrimary));
            style.Setters.Add(new Setter(Control.FontSizeProperty, 13.0));
            return style;
        }

        public static Style SeparatorStyle(Theme t)
        {
            var template = new ControlTemplate(typeof(Separator));
            var line = new FrameworkElementFactory(typeof(Border));
            line.SetValue(Border.BackgroundProperty, t.Border);
            line.SetValue(FrameworkElement.HeightProperty, 1.0);
            line.SetValue(FrameworkElement.MarginProperty, new Thickness(6, 4, 6, 4));
            template.VisualTree = line;

            var style = new Style(typeof(Separator));
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }

        public static ControlTemplate ContextMenuTemplate(Theme t)
        {
            var template = new ControlTemplate(typeof(ContextMenu));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, t.SurfaceBackground);
            border.SetValue(Border.BorderBrushProperty, t.Border);
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetValue(Border.PaddingProperty, new Thickness(4));
            border.AppendChild(new FrameworkElementFactory(typeof(ItemsPresenter)));
            template.VisualTree = border;
            return template;
        }
    }
}
