﻿using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using FluentAvalonia.UI.Media;

namespace FluentAvalonia.Converters;

/// <summary>
/// Converter that converts a color to a SolidColorBrush
/// </summary>
public class ColorToBrushConv : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color c)
            return new SolidColorBrush(c);

        if (value is Color2 c2)
            return new SolidColorBrush(c2);

        return BindingOperations.DoNothing;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ISolidColorBrush sc)
            return sc.Color;

        return BindingOperations.DoNothing;
    }
}
