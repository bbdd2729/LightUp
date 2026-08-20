using LightUpUI.Services;

namespace LightUpTest.Windowing;

public sealed class WindowOwnerPolicyTests
{
    [Fact]
    public void Hidden_window_cannot_be_used_as_owner_for_a_new_window()
    {
        Assert.False(WindowOwnerPolicy.CanUseOwner(isVisible: false));
    }

    [Fact]
    public void Visible_window_can_be_used_as_owner_for_a_new_window()
    {
        Assert.True(WindowOwnerPolicy.CanUseOwner(isVisible: true));
    }
}
