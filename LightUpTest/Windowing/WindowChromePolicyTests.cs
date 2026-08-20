using LightUpUI.Services;

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
}
