using System;
using LightUpUI.Models.Tiles;

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

    public static TileDensityMetrics GetDensityMetrics(TileDensity density)
        => TileDensityPolicy.Normalize(density) == TileDensity.Comfortable
            ? new TileDensityMetrics(188, 160, 56, 14, 9)
            : new TileDensityMetrics(TileWidth, TileHeight, TileIconBoxSize, 12, 7);

    public static bool ShouldShowEmptyState(bool isLoading, bool hasVisibleItems)
        => !isLoading && !hasVisibleItems;

    public static TileLauncherWorkspaceLayout GetWorkspaceLayout(
        CategoryNavigationPlacement placement)
        => placement == CategoryNavigationPlacement.Top
            ? new TileLauncherWorkspaceLayout(0, 0, 10)
            : new TileLauncherWorkspaceLayout(SidebarWidth, 16, 0);
}

public readonly record struct TileLauncherWorkspaceLayout(
    double SidebarWidth,
    double ColumnSpacing,
    double RowSpacing);
