using Avalonia;
using LightUpUI.Models;

namespace LightUpTest.Windowing;

public sealed class LauncherWindowStatePolicyTests
{
    [Fact]
    public void Normalize_keeps_a_saved_window_state_when_it_intersects_a_screen()
    {
        var saved = new LauncherWindowState
        {
            X = 120,
            Y = 80,
            HasPosition = true,
            Width = 720,
            Height = 480,
            IsTopmost = true
        };

        var normalized = LauncherWindowStatePolicy.Normalize(
            saved,
            new PixelRect(0, 0, 1920, 1080),
            new Size(400, 200),
            new Size(1000, 800));

        Assert.Equal(saved.X, normalized.X);
        Assert.Equal(saved.Y, normalized.Y);
        Assert.Equal(saved.Width, normalized.Width);
        Assert.Equal(saved.Height, normalized.Height);
        Assert.True(normalized.IsTopmost);
    }

    [Fact]
    public void Normalize_moves_an_offscreen_window_back_to_the_working_area()
    {
        var normalized = LauncherWindowStatePolicy.Normalize(
            new LauncherWindowState { X = 5000, Y = 5000, HasPosition = true, Width = 720, Height = 480 },
            new PixelRect(0, 0, 1920, 1080),
            new Size(400, 200),
            new Size(1000, 800));

        Assert.Equal(600, normalized.X);
        Assert.Equal(300, normalized.Y);
    }

    [Fact]
    public void Normalize_clamps_invalid_dimensions_to_the_supported_range()
    {
        var normalized = LauncherWindowStatePolicy.Normalize(
            new LauncherWindowState { X = 10, Y = 10, HasPosition = true, Width = 4, Height = 5000 },
            new PixelRect(0, 0, 1920, 1080),
            new Size(400, 200),
            new Size(1000, 800));

        Assert.Equal(400, normalized.Width);
        Assert.Equal(800, normalized.Height);
    }

    [Fact]
    public void Normalize_preserves_center_screen_for_a_state_without_a_saved_position()
    {
        var normalized = LauncherWindowStatePolicy.Normalize(
            new LauncherWindowState { Width = 720, Height = 480 },
            new PixelRect(0, 0, 1920, 1080),
            new Size(400, 200),
            new Size(1000, 800));

        Assert.False(normalized.HasPosition);
    }
}
