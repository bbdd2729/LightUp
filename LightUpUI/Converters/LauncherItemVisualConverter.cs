using System;
using System.Globalization;
using Avalonia.Data.Converters;
using FluentIcons.Common;
using LightUpUI.Models;
using LightUpUI.Models.Tiles;
using LightUpUI.Presentation;

namespace LightUpUI.Converters;

public sealed class LauncherItemIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is LauncherItemKind kind ? LauncherItemVisuals.GetIcon(kind) : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class TileItemIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is TileItemKind kind ? LauncherItemVisuals.GetIcon(kind) : null;

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
