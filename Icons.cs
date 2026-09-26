using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FolderSizeViewer
{
    public static class Icons
    {
        private static Path FromGeometry(string data, Brush fill)
        {
            return new Path
            {
                Data = Geometry.Parse(data),
                Fill = fill,
                Stretch = Stretch.Uniform,
                SnapsToDevicePixels = true
            };
        }

        public static Path ArrowUp(Brush fill)
        {
            return FromGeometry("M12,4 L18,11 L14,11 L14,20 L10,20 L10,11 L6,11 Z", fill);
        }

        public static Path Refresh(Brush fill)
        {
            return FromGeometry(
                "M17.65,6.35C16.2,4.9 14.21,4 12,4c-4.42,0 -7.99,3.58 -7.99,8s3.57,8 7.99,8c3.73,0 6.84,-2.55 7.73,-6h-2.08c-0.82,2.33 -3.04,4 -5.65,4c-3.31,0 -6,-2.69 -6,-6s2.69,-6 6,-6c1.66,0 3.14,0.69 4.22,1.78L13,11h7V4l-2.35,2.35z",
                fill);
        }

        public static Path Search(Brush fill)
        {
            return FromGeometry(
                "M15.5,14h-0.79l-0.28,-0.27C15.41,12.59 16,11.11 16,9.5C16,5.91 13.09,3 9.5,3S3,5.91 3,9.5S5.91,16 9.5,16c1.61,0 3.09,-0.59 4.23,-1.57l0.27,0.28v0.79l5,4.99L20.49,19l-4.99,-5z M9.5,14C7.01,14 5,11.99 5,9.5S7.01,5 9.5,5S14,7.01 14,9.5S11.99,14 9.5,14z",
                fill);
        }

        public static Path Folder(Brush fill)
        {
            return FromGeometry("M3,6 L9,6 L11,8 L21,8 L21,18 L3,18 Z", fill);
        }

        public static Path File(Brush fill)
        {
            return FromGeometry("M6,3 L15,3 L18,6 L18,21 L6,21 Z", fill);
        }

        public static UIElement Drive(Brush fill)
        {
            var grid = new System.Windows.Controls.Grid { Width = 24, Height = 24 };
            var body = new Rectangle
            {
                Width = 22,
                Height = 12,
                RadiusX = 3,
                RadiusY = 3,
                Fill = fill,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var slot = new Rectangle
            {
                Width = 10,
                Height = 2,
                Fill = Brushes.White,
                Opacity = 0.55,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, -3, 0, 0)
            };
            grid.Children.Add(body);
            grid.Children.Add(slot);
            return grid;
        }

        public static UIElement Sun(Brush fill)
        {
            var grid = new System.Windows.Controls.Grid { Width = 20, Height = 20 };
            grid.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = fill, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            for (int i = 0; i < 8; i++)
            {
                var ray = new Rectangle
                {
                    Width = 2,
                    Height = 4,
                    Fill = fill,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    RadiusX = 1,
                    RadiusY = 1,
                    RenderTransformOrigin = new Point(0.5, 5)
                };
                ray.RenderTransform = new RotateTransform(i * 45);
                grid.Children.Add(ray);
            }
            return grid;
        }

        public static UIElement Moon(Brush fill)
        {
            var big = new EllipseGeometry(new Point(10, 10), 8, 8);
            var small = new EllipseGeometry(new Point(14, 7), 7, 7);
            var combined = new CombinedGeometry(GeometryCombineMode.Exclude, big, small);
            return new Path { Data = combined, Fill = fill, Width = 20, Height = 20, Stretch = Stretch.Uniform };
        }
    }
}
