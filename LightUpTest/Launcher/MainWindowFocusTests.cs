using LightUpUI.Views;

namespace LightUpTest.Launcher;

public sealed class MainWindowFocusTests
{
    [Fact]
    public void TryFocusQueryBox_returns_false_when_xaml_control_is_not_ready()
    {
        Assert.False(MainWindow.TryFocusQueryBox(null));
    }
}
