using System.Collections.Generic;
using LightUpUI.Models.Tiles;

namespace LightUpUI.Models;

public sealed class SearchLauncherSettings
{
    // Keep Full as the migration-safe default until the tile window is available to populate entries.
    public SearchLauncherMode Mode { get; set; } = SearchLauncherMode.Full;
    public string Hotkey { get; set; } = "alt+space";
    public string TileLauncherHotkey { get; set; } = "alt+shift+space";
    public bool LaunchAtStartup { get; set; }
    public bool EnableTileCornerTrigger { get; set; }
    public int TileCornerTriggerDelayMilliseconds { get; set; } = 700;
    public bool CloseTileLauncherOnPointerLeave { get; set; } = true;
    public bool CloseTileLauncherAfterLaunch { get; set; } = true;
    public int MaxResults { get; set; } = 30;
    public bool SearchAllTileCategories { get; set; } = true;
    public bool SaveQueryHistory { get; set; } = true;
    public List<string> QueryHistory { get; set; } = [];
    public CategoryNavigationPlacement CategoryNavigationPlacement { get; set; } = CategoryNavigationPlacement.Left;
    public LauncherAppearanceSettings Appearance { get; set; } = new();
    public Dictionary<string, PluginSettings> Plugins { get; set; } = [];
}

public sealed class PluginSettings
{
    public bool IsEnabled { get; set; } = true;
    public int Weight { get; set; }
}
