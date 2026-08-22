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
        settings.Appearance.ThemeMode = ThemePalettePolicy.NormalizeThemeMode(settings.Appearance.ThemeMode);
        settings.Appearance.ColorPalette = ThemePalettePolicy.NormalizeColorPalette(settings.Appearance.ColorPalette);
        settings.Appearance.CustomAccentColor = ThemePalettePolicy.NormalizeCustomAccentColor(
            settings.Appearance.CustomAccentColor);
        settings.Appearance.TileDensity = TileDensityPolicy.Normalize(settings.Appearance.TileDensity);
        settings.MaxResults = SearchResultLimitPolicy.Normalize(settings.MaxResults);
        settings.QueryHistory = [.. SearchHistoryPolicy.Normalize(settings.QueryHistory)];
        settings.Hotkey = string.IsNullOrWhiteSpace(settings.Hotkey) ? "alt+space" : settings.Hotkey;
        settings.TileLauncherHotkey = string.IsNullOrWhiteSpace(settings.TileLauncherHotkey)
            ? "alt+shift+space"
            : settings.TileLauncherHotkey;
        settings.TrayIconLeftClickAction = settings.TrayIconLeftClickAction is
            TrayIconLeftClickAction.Search or TrayIconLeftClickAction.Tiles
            ? settings.TrayIconLeftClickAction
            : TrayIconLeftClickAction.Search;
        settings.TileCornerTriggerDelayMilliseconds = TileCornerTriggerSettingsPolicy.NormalizeDelay(
            settings.TileCornerTriggerDelayMilliseconds);
        settings.Plugins ??= [];
        settings.Appearance.SearchWindow ??= new LauncherAppearanceSettings().SearchWindow;
        settings.Appearance.TileLauncherWindow ??= new LauncherAppearanceSettings().TileLauncherWindow;
        return settings;
    }
}
