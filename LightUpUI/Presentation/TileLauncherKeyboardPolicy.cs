using Avalonia.Input;

namespace LightUpUI.Presentation;

public static class TileLauncherKeyboardPolicy
{
    public static bool ShouldRemoveSelectedItem(Key key, bool isTextEditing)
        => key == Key.Delete && !isTextEditing;
}
