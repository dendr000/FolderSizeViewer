// Copyright (c) dendr000. MIT License.
using System.Windows;
using System.Windows.Media;

namespace FolderSizeViewer
{
    // assets/icons/*.svg 로더(SvgIcon)를 아이콘 이름별로 감싸는 얇은 창구.
    internal static class Icons
    {
        public static FrameworkElement ArrowUp(Brush fill) { return SvgIcon.Load("arrow-up.svg", fill); }
        public static FrameworkElement Refresh(Brush fill) { return SvgIcon.Load("refresh.svg", fill); }
        public static FrameworkElement Search(Brush fill) { return SvgIcon.Load("search.svg", fill); }
        public static FrameworkElement Sun(Brush fill) { return SvgIcon.Load("sun.svg", fill); }
        public static FrameworkElement Moon(Brush fill) { return SvgIcon.Load("moon.svg", fill); }
        public static FrameworkElement Drive(Brush fill) { return SvgIcon.Load("drive.svg", fill); }

        public static Geometry FolderGeometry() { return SvgIcon.LoadGeometry("folder.svg"); }
        public static Geometry FileGeometry() { return SvgIcon.LoadGeometry("file.svg"); }
    }
}
