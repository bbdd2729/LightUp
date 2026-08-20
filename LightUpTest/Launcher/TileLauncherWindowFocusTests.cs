using LightUpUI.Views;
using Avalonia.Controls;

namespace LightUpTest.Launcher;

public sealed class TileLauncherWindowFocusTests
{
    [Fact]
    public void TryFocusSearchBox_returns_false_when_xaml_control_is_not_ready()
    {
        Assert.False(TileLauncherWindow.TryFocusSearchBox(null));
    }

    [Fact]
    public void TryFocusSearchBox_does_not_throw_for_a_detached_control()
    {
        var exception = Record.Exception(() => TileLauncherWindow.TryFocusSearchBox(new TextBox()));

        Assert.Null(exception);
    }

    [Fact]
    public void TryFocusTileTitleBox_returns_false_when_xaml_control_is_not_ready()
    {
        Assert.False(TileLauncherWindow.TryFocusTileTitleBox(null));
    }

    [Fact]
    public void TryFocusTileTitleBox_does_not_throw_for_a_detached_control()
    {
        var exception = Record.Exception(() => TileLauncherWindow.TryFocusTileTitleBox(new TextBox()));

        Assert.Null(exception);
    }

    [Fact]
    public void TryFocusTileNotesBox_returns_false_when_xaml_control_is_not_ready()
    {
        Assert.False(TileLauncherWindow.TryFocusTileNotesBox(null));
    }

    [Fact]
    public void TryFocusTileNotesBox_does_not_throw_for_a_detached_control()
    {
        var exception = Record.Exception(() => TileLauncherWindow.TryFocusTileNotesBox(new TextBox()));

        Assert.Null(exception);
    }
}
