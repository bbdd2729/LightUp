using System;
using System.Globalization;
using Avalonia.Data.Converters;
using FluentIcons.Common;
using LightUpUI.Models;
using LightUpUI.Models.Tiles;
using LightUpUI.Presentation;
using LightUpUI.Services;

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

public sealed class LauncherItemNativeIconConverter : IValueConverter
{
    private readonly IFileIconService _iconService;

    public LauncherItemNativeIconConverter()
        : this(new WindowsFileIconService())
    {
    }

    public LauncherItemNativeIconConverter(IFileIconService iconService) => _iconService = iconService;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is LauncherItem item
            ? _iconService.GetIcon(item.IconPath, item.LaunchPath, 32)
            : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class TileItemNativeIconConverter : IValueConverter
{
    private readonly IFileIconService _iconService;

    public TileItemNativeIconConverter()
        : this(new WindowsFileIconService())
    {
    }

    public TileItemNativeIconConverter(IFileIconService iconService) => _iconService = iconService;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is TileItem item
            ? _iconService.GetIcon(item.CustomIconPath, item.TargetPath, 48)
            : null;

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
