using System;

namespace LightUpUI.Models.Tiles;

public enum TileItemKind
{
    Application,
    File,
    Folder,
    Shortcut,
    Url
}

public sealed class TileItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string? Arguments { get; set; }
    public TileItemKind Kind { get; set; } = TileItemKind.Shortcut;
    public int SortOrder { get; set; }
    public int LaunchCount { get; set; }
    public DateTime? LastLaunchedAtUtc { get; set; }
    public string? CustomIconPath { get; set; }
    public string? Notes { get; set; }
}
