using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Celestite.Utils.Converters
{
    public class FileSizeConverter : IValueConverter
    {
        public static readonly FileSizeConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is long fileSize)
                return FileUtils.CalcFileSizeString(fileSize);
            throw new NotSupportedException();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
