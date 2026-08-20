using LightUpUI.Presentation;

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
}
