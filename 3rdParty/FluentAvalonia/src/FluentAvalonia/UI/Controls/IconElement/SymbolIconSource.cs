﻿using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace FluentAvalonia.UI.Controls.IconElement;

[TypeConverter(typeof(SymbolIconSourceConverter))]
public class SymbolIconSource : PathIconSource
{
    private static readonly IGlyphTypeface _typeface = new Typeface("avares://FluentAvalonia/Fonts#FluentSystemIcons-Resizable").GlyphTypeface;

    public static readonly StyledProperty<Symbol> SymbolProperty = SymbolIcon.SymbolProperty.AddOwner<SymbolIconSource>();
    public static readonly StyledProperty<bool> IsFilledProperty = SymbolIcon.IsFilledProperty.AddOwner<SymbolIconSource>();
    public static readonly StyledProperty<double> FontSizeProperty = TextElement.FontSizeProperty.AddOwner<SymbolIconSource>();

    public SymbolIconSource()
    {
        base.Stretch = Stretch.None;
        FontSize = 20; // Override value inherited from visual parents.
        InvalidateData();
    }

    public Symbol Symbol
    {
        get => GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    public bool IsFilled
    {
        get => GetValue(IsFilledProperty);
        set => SetValue(IsFilledProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SymbolProperty || change.Property == IsFilledProperty)
        {
            InvalidateData();
        }
    }

    private void InvalidateData()
    {
        var codepoint = Symbol.ToChar(IsFilled);
        var glyphRun = new GlyphRun(_typeface, FontSize, new[] { codepoint }, new[] { _typeface.GetGlyph(codepoint) });
        base.Data = glyphRun.BuildGeometry();
    }

    [Obsolete("Do not use.")]
    public new static readonly StyledProperty<Geometry> DataProperty =
        AvaloniaProperty.Register<SymbolIconSource, Geometry>(nameof(Data));

    [Obsolete("Do not use.")] public new Geometry Data { get => base.Data; set { } }
}

public class SymbolIconSourceConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        if (sourceType == typeof(string) || sourceType == typeof(Symbol))
        {
            return true;
        }
        return base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string val && SymbolExtensions.TryParse(val, out var pSymbol))
        {
            return new SymbolIconSource { Symbol = pSymbol };
        }
        else if (value is Symbol symbol)
        {
            return new SymbolIconSource { Symbol = symbol };
        }
        return base.ConvertFrom(context, culture, value);
    }
}
