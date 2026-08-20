using LightUpUI.Models;
using LightUpUI.Models.Tiles;
using FluentIcons.Common;

namespace LightUpUI.Presentation;

public static class LauncherItemVisuals
{
    public static Icon GetIcon(LauncherItemKind kind) => kind switch
    {
        LauncherItemKind.Application => Icon.AppGeneric,
        LauncherItemKind.Shortcut => Icon.Link,
        LauncherItemKind.PathExecutable => Icon.WindowDevTools,
        LauncherItemKind.Action => Icon.Sparkle,
        _ => Icon.Circle
    };

    public static Icon GetIcon(TileItemKind kind) => kind switch
    {
        TileItemKind.Application => Icon.AppGeneric,
        TileItemKind.File => Icon.Document,
        TileItemKind.Folder => Icon.Folder,
        TileItemKind.Shortcut => Icon.Link,
        TileItemKind.Url => Icon.Globe,
        _ => Icon.Circle
    };

    public static string GetLabel(LauncherItemKind kind) => kind switch
    {
        LauncherItemKind.Application => "应用",
        LauncherItemKind.Shortcut => "快捷方式",
        LauncherItemKind.PathExecutable => "系统程序",
        LauncherItemKind.Action => "功能",
        _ => "项目"
    };
}
