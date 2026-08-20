using Avalonia;

namespace LightUpUI.Models;

public static class LauncherWindowStatePolicy
{
    public static LauncherWindowState Normalize(
        LauncherWindowState? state,
        PixelRect workingArea,
        Size minimumSize,
        Size maximumSize)
    {
        state ??= new LauncherWindowState();
        var width = Clamp(state.Width, (int)minimumSize.Width, (int)maximumSize.Width);
        var height = Clamp(state.Height, (int)minimumSize.Height, (int)maximumSize.Height);
        var x = state.X;
        var y = state.Y;
        var hasPosition = state.HasPosition;
        var savedBounds = new PixelRect(x, y, width, height);
        if (hasPosition && !Intersects(savedBounds, workingArea))
        {
            x = workingArea.X + (workingArea.Width - width) / 2;
            y = workingArea.Y + (workingArea.Height - height) / 2;
        }

        return new LauncherWindowState
        {
            HasPosition = hasPosition,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            IsTopmost = state.IsTopmost
        };
    }

    private static int Clamp(int value, int minimum, int maximum)
        => value < minimum ? minimum : value > maximum ? maximum : value;

    private static bool Intersects(PixelRect first, PixelRect second)
        => first.X < second.Right && first.Right > second.X && first.Y < second.Bottom && first.Bottom > second.Y;
}
