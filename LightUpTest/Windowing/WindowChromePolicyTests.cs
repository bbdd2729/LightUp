using LightUpUI.Services;
using FluentIcons.Common;

namespace LightUpTest.Windowing;

public sealed class WindowChromePolicyTests
{
    [Fact]
    public void Toggling_topmost_inverts_the_current_state()
    {
        Assert.False(WindowChromePolicy.ToggleTopmost(true));
        Assert.True(WindowChromePolicy.ToggleTopmost(false));
    }

    [Fact]
    public void Title_bar_drag_is_allowed_only_for_the_drag_surface()
    {
        Assert.True(WindowChromePolicy.CanStartMoveDrag(isInteractiveChild: false));
        Assert.False(WindowChromePolicy.CanStartMoveDrag(isInteractiveChild: true));
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    public void Deactivation_only_hides_transient_unpinned_windows(
        bool isTopmost,
        bool staysOpenWhenDeactivated,
        bool expected)
    {
        Assert.Equal(expected, WindowChromePolicy.ShouldHideOnDeactivated(isTopmost, staysOpenWhenDeactivated));
    }

    [Fact]
    public void Topmost_state_uses_filled_icon_and_explicit_feedback()
    {
        Assert.Equal(IconVariant.Filled, WindowChromePolicy.GetTopmostIconVariant(true));
        Assert.Equal(IconVariant.Regular, WindowChromePolicy.GetTopmostIconVariant(false));
        Assert.Equal("已置顶，点击取消置顶", WindowChromePolicy.GetTopmostToolTip(true));
        Assert.Equal("置顶窗口", WindowChromePolicy.GetTopmostToolTip(false));
    }
}
