using LightUpUI.Views;
using Avalonia.Controls;

namespace LightUpTest.Launcher;

public sealed class MainWindowFocusTests
{
    [Fact]
    public void TryFocusQueryBox_returns_false_when_xaml_control_is_not_ready()
    {
        Assert.False(MainWindow.TryFocusQueryBox(null));
    }

    [Fact]
    public void TryFocusQueryBox_does_not_throw_for_a_detached_control()
    {
        var exception = Record.Exception(() => MainWindow.TryFocusQueryBox(new TextBox()));

        Assert.Null(exception);
    }
}
