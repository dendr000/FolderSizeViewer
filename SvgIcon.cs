// Copyright (c) dendr000. MIT License.
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace FolderSizeViewer
{
    // assets/icons/*.svg 를 읽어 WPF 도형으로 변환한다.
    // 아이콘 전용으로 만든 단순한 SVG 파일만 지원하는 최소 구현이다 (path/circle/rect,
    // fill-rule=evenodd, rotate(a,cx,cy) 변환) — 임의의 SVG를 전부 지원하는 범용 렌더러가 아니다.
    internal static class SvgIcon
    {
        private static XElement LoadRoot(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(name))
            {
                if (stream == null) throw new FileNotFoundException("embedded icon not found: " + name);
                return XDocument.Load(stream).Root;
            }
        }

        private static double Attr(XElement el, string name, double fallback)
        {
            var a = el.Attribute(name);
            return a == null ? fallback : double.Parse(a.Value, CultureInfo.InvariantCulture);
        }

        private static string PathDataWithFillRule(XElement pathEl)
        {
            string d = (string)pathEl.Attribute("d");
            var fillRule = pathEl.Attribute("fill-rule");
            string prefix = (fillRule != null && fillRule.Value == "evenodd") ? "F0 " : "";
            return prefix + d;
        }

        // 단일 <path> 로만 이루어진 아이콘의 Geometry만 필요할 때 사용.
        public static Geometry LoadGeometry(string name)
        {
            var root = LoadRoot(name);
            var ns = root.Name.Namespace;
            var pathEl = root.Element(ns + "path");
            return Geometry.Parse(PathDataWithFillRule(pathEl));
        }

        // path/circle/rect가 섞인 아이콘까지 지원하는 완전한 로더. 색은 fill="currentColor" 자리에 적용된다.
        public static FrameworkElement Load(string name, Brush fill)
        {
            var root = LoadRoot(name);
            var ns = root.Name.Namespace;

            double vw = Attr(root, "width", 24);
            double vh = Attr(root, "height", 24);
            var viewBox = root.Attribute("viewBox");
            if (viewBox != null)
            {
                var parts = viewBox.Value.Split(' ');
                vw = double.Parse(parts[2], CultureInfo.InvariantCulture);
                vh = double.Parse(parts[3], CultureInfo.InvariantCulture);
            }

            var canvas = new System.Windows.Controls.Canvas { Width = vw, Height = vh };

            foreach (var el in root.Elements())
            {
                Shape shape;
                double originX = 0, originY = 0;

                if (el.Name == ns + "path")
                {
                    shape = new System.Windows.Shapes.Path { Data = Geometry.Parse(PathDataWithFillRule(el)) };
                }
                else if (el.Name == ns + "circle")
                {
                    double cx = Attr(el, "cx", 0), cy = Attr(el, "cy", 0), r = Attr(el, "r", 0);
                    shape = new Ellipse { Width = r * 2, Height = r * 2 };
                    originX = cx - r;
                    originY = cy - r;
                    System.Windows.Controls.Canvas.SetLeft(shape, originX);
                    System.Windows.Controls.Canvas.SetTop(shape, originY);
                }
                else if (el.Name == ns + "rect")
                {
                    double x = Attr(el, "x", 0), y = Attr(el, "y", 0);
                    double w = Attr(el, "width", 0), h = Attr(el, "height", 0);
                    var rect = new Rectangle { Width = w, Height = h };
                    var rx = el.Attribute("rx");
                    if (rx != null)
                    {
                        rect.RadiusX = double.Parse(rx.Value, CultureInfo.InvariantCulture);
                        rect.RadiusY = rect.RadiusX;
                    }
                    originX = x;
                    originY = y;
                    System.Windows.Controls.Canvas.SetLeft(rect, x);
                    System.Windows.Controls.Canvas.SetTop(rect, y);

                    var transform = el.Attribute("transform");
                    if (transform != null) ApplyRotate(rect, transform.Value, x, y);

                    shape = rect;
                }
                else
                {
                    continue;
                }

                var fillAttr = el.Attribute("fill");
                shape.Fill = (fillAttr == null || fillAttr.Value == "currentColor")
                    ? fill
                    : (Brush)new BrushConverter().ConvertFromString(fillAttr.Value);

                var opacityAttr = el.Attribute("opacity");
                if (opacityAttr != null) shape.Opacity = double.Parse(opacityAttr.Value, CultureInfo.InvariantCulture);

                canvas.Children.Add(shape);
            }

            return new System.Windows.Controls.Viewbox { Child = canvas, Stretch = Stretch.Uniform };
        }

        // "rotate(angle cx cy)" 형식만 지원 (우리 아이콘에서 쓰는 전부).
        private static void ApplyRotate(Shape shape, string transform, double localOriginX, double localOriginY)
        {
            if (!transform.StartsWith("rotate(")) return;
            string inner = transform.Substring(7, transform.Length - 8);
            var parts = inner.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            double angle = double.Parse(parts[0], CultureInfo.InvariantCulture);
            double cx = double.Parse(parts[1], CultureInfo.InvariantCulture);
            double cy = double.Parse(parts[2], CultureInfo.InvariantCulture);
            shape.RenderTransform = new RotateTransform(angle, cx - localOriginX, cy - localOriginY);
        }
    }
}
