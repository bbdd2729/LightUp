using LightUpUI.Models.Tiles;

namespace LightUpUI.Models;

public static class SearchLauncherSettingsPolicy
{
    public static SearchLauncherSettings Normalize(SearchLauncherSettings? settings)
    {
        settings ??= new SearchLauncherSettings();
        settings.Mode = settings.Mode is SearchLauncherMode.Simple or SearchLauncherMode.Full
            ? settings.Mode
            : SearchLauncherMode.Full;
        settings.CategoryNavigationPlacement = settings.CategoryNavigationPlacement is
            CategoryNavigationPlacement.Left or CategoryNavigationPlacement.Top
            ? settings.CategoryNavigationPlacement
            : CategoryNavigationPlacement.Left;
        settings.Appearance ??= new LauncherAppearanceSettings();
        settings.Appearance.TileDensity = TileDensityPolicy.Normalize(settings.Appearance.TileDensity);
        settings.MaxResults = SearchResultLimitPolicy.Normalize(settings.MaxResults);
        settings.Hotkey = string.IsNullOrWhiteSpace(settings.Hotkey) ? "alt+space" : settings.Hotkey;
        settings.TileLauncherHotkey = string.IsNullOrWhiteSpace(settings.TileLauncherHotkey)
            ? "alt+shift+space"
            : settings.TileLauncherHotkey;
        settings.Plugins ??= [];
        settings.Appearance.SearchWindow ??= new LauncherAppearanceSettings().SearchWindow;
        settings.Appearance.TileLauncherWindow ??= new LauncherAppearanceSettings().TileLauncherWindow;
        return settings;
    }
}
