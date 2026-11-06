using System;
using System.Globalization;
using System.Windows.Data;

namespace AnyCAD.WPF
{
    /// <summary>string -> bool：非空字符串返回 true，null/空串返回 false。</summary>
    internal sealed class StringIsNonEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => !string.IsNullOrEmpty(value as string);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
