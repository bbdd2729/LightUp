namespace LightUpUI.Models.Tiles;

public static class CategoryNavigationPlacementPolicy
{
    public static CategoryNavigationPlacement Normalize(CategoryNavigationPlacement placement)
        => placement == CategoryNavigationPlacement.Top
            ? CategoryNavigationPlacement.Top
            : CategoryNavigationPlacement.Left;
}
