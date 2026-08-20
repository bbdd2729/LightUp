using System;
using System.Globalization;
using Avalonia.Data.Converters;
using LightUpUI.Models;
using LightUpUI.Presentation;

namespace LightUpUI.Converters;

public sealed class LauncherItemGlyphConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is LauncherItemKind kind ? LauncherItemVisuals.GetGlyph(kind) : "•";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class LauncherItemLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is LauncherItemKind kind ? LauncherItemVisuals.GetLabel(kind) : "项目";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
