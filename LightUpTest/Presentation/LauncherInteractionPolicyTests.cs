using LightUpUI.Presentation;

namespace LightUpTest.Presentation;

public sealed class LauncherInteractionPolicyTests
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void A_result_launches_only_after_a_double_click(int clickCount, bool expected)
    {
        Assert.Equal(expected, LauncherInteractionPolicy.ShouldLaunchOnClick(clickCount));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void A_result_is_selectable_from_any_pointer_click(int clickCount, bool expected)
    {
        Assert.Equal(expected, LauncherInteractionPolicy.ShouldSelectOnClick(clickCount));
    }
}
