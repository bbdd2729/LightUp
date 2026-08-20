using System;

namespace LightUpUI.Presentation;

public static class TileLauncherLayoutPolicy
{
    public const double SearchWidthRatio = 0.5;
    public const double SidebarWidth = 204;
    public const double TileWidth = 156;
    public const double TileHeight = 132;
    public const double TileIconBoxSize = 44;

    public static double GetSearchMaxWidth(double launcherWidth)
        => double.IsFinite(launcherWidth) && launcherWidth > 0
            ? launcherWidth * SearchWidthRatio
            : 0;

    public static bool ShouldShowEmptyState(bool isLoading, bool hasVisibleItems)
        => !isLoading && !hasVisibleItems;
}
