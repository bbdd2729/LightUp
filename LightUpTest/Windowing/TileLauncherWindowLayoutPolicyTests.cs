using LightUpUI.Presentation;
using LightUpUI.Models.Tiles;

namespace LightUpTest.Windowing;

public sealed class TileLauncherWindowLayoutPolicyTests
{
    [Theory]
    [InlineData(720, 360)]
    [InlineData(980, 490)]
    [InlineData(1280, 640)]
    public void Search_max_width_is_half_of_the_launcher_width(double launcherWidth, double expected)
    {
        Assert.Equal(expected, TileLauncherLayoutPolicy.GetSearchMaxWidth(launcherWidth));
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void Empty_state_is_visible_only_after_loading_with_no_items(
        bool isLoading,
        bool hasVisibleItems,
        bool expected)
    {
        Assert.Equal(
            expected,
            TileLauncherLayoutPolicy.ShouldShowEmptyState(isLoading, hasVisibleItems));
    }

    [Fact]
    public void Search_width_is_zero_for_invalid_window_widths()
    {
        Assert.Equal(0, TileLauncherLayoutPolicy.GetSearchMaxWidth(0));
        Assert.Equal(0, TileLauncherLayoutPolicy.GetSearchMaxWidth(-1));
        Assert.Equal(0, TileLauncherLayoutPolicy.GetSearchMaxWidth(double.NaN));
    }

    [Theory]
    [InlineData(CategoryNavigationPlacement.Left, 204, 16, 0)]
    [InlineData(CategoryNavigationPlacement.Top, 0, 0, 10)]
    public void Workspace_layout_uses_the_expected_space_for_each_navigation_placement(
        CategoryNavigationPlacement placement,
        double expectedSidebarWidth,
        double expectedColumnSpacing,
        double expectedRowSpacing)
    {
        var layout = TileLauncherLayoutPolicy.GetWorkspaceLayout(placement);

        Assert.Equal(expectedSidebarWidth, layout.SidebarWidth);
        Assert.Equal(expectedColumnSpacing, layout.ColumnSpacing);
        Assert.Equal(expectedRowSpacing, layout.RowSpacing);
    }
}
