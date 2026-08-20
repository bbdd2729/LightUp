using LightUpUI.Models;

namespace LightUpUI.Presentation;

public static class LauncherItemVisuals
{
    public static string GetGlyph(LauncherItemKind kind) => kind switch
    {
        LauncherItemKind.Application => "▣",
        LauncherItemKind.Shortcut => "↗",
        LauncherItemKind.PathExecutable => "⌘",
        LauncherItemKind.Action => "✦",
        _ => "•"
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
