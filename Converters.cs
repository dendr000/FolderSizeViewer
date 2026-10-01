// Copyright (c) dendr000. MIT License.
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FolderSizeViewer
{
    public class FractionToWidthConverter : IValueConverter
    {
        public double MaxWidth = 90;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double fraction = value is double ? (double)value : 0.0;
            if (fraction < 0) fraction = 0;
            if (fraction > 1) fraction = 1;
            return Math.Max(2.0, fraction * MaxWidth);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool && (bool)value;
            if (Invert) b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
