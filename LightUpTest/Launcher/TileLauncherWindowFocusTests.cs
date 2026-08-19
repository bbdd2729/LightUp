using LightUpUI.Views;

namespace LightUpTest.Launcher;

public sealed class TileLauncherWindowFocusTests
{
    [Fact]
    public void TryFocusSearchBox_returns_false_when_xaml_control_is_not_ready()
    {
        Assert.False(TileLauncherWindow.TryFocusSearchBox(null));
    }
}
