// Copyright (c) dendr000. MIT License.
using System;
using System.Windows.Media;
using Microsoft.Win32;

namespace FolderSizeViewer
{
    public class Theme
    {
        public readonly SolidColorBrush WindowBackground = new SolidColorBrush();
        public readonly SolidColorBrush SurfaceBackground = new SolidColorBrush();
        public readonly SolidColorBrush ToolbarBackground = new SolidColorBrush();
        public readonly SolidColorBrush Border = new SolidColorBrush();
        public readonly SolidColorBrush TextPrimary = new SolidColorBrush();
        public readonly SolidColorBrush TextSecondary = new SolidColorBrush();
        public readonly SolidColorBrush Accent = new SolidColorBrush();
        public readonly SolidColorBrush AccentSoft = new SolidColorBrush();
        public readonly SolidColorBrush RowHover = new SolidColorBrush();
        public readonly SolidColorBrush RowAlternate = new SolidColorBrush();
        public readonly SolidColorBrush RowSelected = new SolidColorBrush();
        public readonly SolidColorBrush ControlBackground = new SolidColorBrush();
        public readonly SolidColorBrush ControlHover = new SolidColorBrush();
        public readonly SolidColorBrush BarTrack = new SolidColorBrush();

        private struct Palette
        {
            public Color WindowBackground, SurfaceBackground, ToolbarBackground, Border, TextPrimary, TextSecondary,
                Accent, AccentSoft, RowHover, RowAlternate, RowSelected, ControlBackground, ControlHover, BarTrack;
        }

        private static readonly Palette LightPalette = new Palette
        {
            WindowBackground = Color.FromRgb(0xF7, 0xF7, 0xFA),
            SurfaceBackground = Color.FromRgb(0xFF, 0xFF, 0xFF),
            ToolbarBackground = Color.FromRgb(0xFF, 0xFF, 0xFF),
            Border = Color.FromRgb(0xE3, 0xE3, 0xE8),
            TextPrimary = Color.FromRgb(0x1E, 0x1E, 0x24),
            TextSecondary = Color.FromRgb(0x6B, 0x6B, 0x76),
            Accent = Color.FromRgb(0x4C, 0x6E, 0xF5),
            AccentSoft = Color.FromRgb(0xE8, 0xED, 0xFE),
            RowHover = Color.FromRgb(0xF0, 0xF2, 0xFA),
            RowAlternate = Color.FromRgb(0xFA, 0xFA, 0xFD),
            RowSelected = Color.FromRgb(0xDD, 0xE6, 0xFD),
            ControlBackground = Color.FromRgb(0xF1, 0xF2, 0xF6),
            ControlHover = Color.FromRgb(0xE6, 0xE8, 0xF0),
            BarTrack = Color.FromRgb(0xC9, 0xD3, 0xFB),
        };

        private static readonly Palette DarkPalette = new Palette
        {
            WindowBackground = Color.FromRgb(0x1A, 0x1B, 0x1F),
            SurfaceBackground = Color.FromRgb(0x23, 0x24, 0x29),
            ToolbarBackground = Color.FromRgb(0x23, 0x24, 0x29),
            Border = Color.FromRgb(0x35, 0x36, 0x3D),
            TextPrimary = Color.FromRgb(0xF0, 0xF0, 0xF4),
            TextSecondary = Color.FromRgb(0x9A, 0x9B, 0xA6),
            Accent = Color.FromRgb(0x6D, 0x8B, 0xFF),
            AccentSoft = Color.FromRgb(0x2A, 0x30, 0x4A),
            RowHover = Color.FromRgb(0x2C, 0x2E, 0x36),
            RowAlternate = Color.FromRgb(0x1F, 0x20, 0x25),
            RowSelected = Color.FromRgb(0x33, 0x3C, 0x5C),
            ControlBackground = Color.FromRgb(0x2A, 0x2B, 0x32),
            ControlHover = Color.FromRgb(0x34, 0x36, 0x3F),
            BarTrack = Color.FromRgb(0x40, 0x47, 0x6B),
        };

        public bool IsDark { get; private set; }

        public Theme(bool isDark)
        {
            IsDark = isDark;
            ApplyImmediate(isDark ? DarkPalette : LightPalette);
        }

        private void ApplyImmediate(Palette p)
        {
            WindowBackground.Color = p.WindowBackground;
            SurfaceBackground.Color = p.SurfaceBackground;
            ToolbarBackground.Color = p.ToolbarBackground;
            Border.Color = p.Border;
            TextPrimary.Color = p.TextPrimary;
            TextSecondary.Color = p.TextSecondary;
            Accent.Color = p.Accent;
            AccentSoft.Color = p.AccentSoft;
            RowHover.Color = p.RowHover;
            RowAlternate.Color = p.RowAlternate;
            RowSelected.Color = p.RowSelected;
            ControlBackground.Color = p.ControlBackground;
            ControlHover.Color = p.ControlHover;
            BarTrack.Color = p.BarTrack;
        }

        public void SwitchTo(bool isDark, TimeSpan duration)
        {
            if (IsDark == isDark) return;
            IsDark = isDark;
            var p = isDark ? DarkPalette : LightPalette;

            Animate(WindowBackground, p.WindowBackground, duration);
            Animate(SurfaceBackground, p.SurfaceBackground, duration);
            Animate(ToolbarBackground, p.ToolbarBackground, duration);
            Animate(Border, p.Border, duration);
            Animate(TextPrimary, p.TextPrimary, duration);
            Animate(TextSecondary, p.TextSecondary, duration);
            Animate(Accent, p.Accent, duration);
            Animate(AccentSoft, p.AccentSoft, duration);
            Animate(RowHover, p.RowHover, duration);
            Animate(RowAlternate, p.RowAlternate, duration);
            Animate(RowSelected, p.RowSelected, duration);
            Animate(ControlBackground, p.ControlBackground, duration);
            Animate(ControlHover, p.ControlHover, duration);
            Animate(BarTrack, p.BarTrack, duration);
        }

        private static void Animate(SolidColorBrush brush, Color target, TimeSpan duration)
        {
            var anim = new System.Windows.Media.Animation.ColorAnimation(target, duration)
            {
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase()
            };
            brush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
        }

        public static bool IsSystemInDarkMode()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key == null) return false;
                    var value = key.GetValue("AppsUseLightTheme");
                    if (value is int) return ((int)value) == 0;
                    return false;
                }
            }
            catch { return false; }
        }
    }
}
