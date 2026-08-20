using Avalonia.Input;
using LightUpUI.Presentation;

namespace LightUpTest.Presentation;

public sealed class TileLauncherKeyboardPolicyTests
{
    [Fact]
    public void ShouldRemoveSelectedItem_accepts_delete_outside_text_editing()
    {
        Assert.True(TileLauncherKeyboardPolicy.ShouldRemoveSelectedItem(Key.Delete, isTextEditing: false));
    }

    [Fact]
    public void ShouldRemoveSelectedItem_rejects_delete_while_editing_text()
    {
        Assert.False(TileLauncherKeyboardPolicy.ShouldRemoveSelectedItem(Key.Delete, isTextEditing: true));
    }

    [Theory]
    [InlineData(Key.Back)]
    [InlineData(Key.Enter)]
    [InlineData(Key.F2)]
    public void ShouldRemoveSelectedItem_rejects_other_keys(Key key)
    {
        Assert.False(TileLauncherKeyboardPolicy.ShouldRemoveSelectedItem(key, isTextEditing: false));
    }
}
