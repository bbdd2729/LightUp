using Avalonia;
using LightUpUI.Services;

namespace LightUpTest.Windowing;

public sealed class TrayMenuPlacementPolicyTests
{
    [Fact]
    public void Places_menu_inside_the_working_area_near_the_taskbar_corner()
    {
        var position = TrayMenuPlacementPolicy.GetPosition(
            new PixelRect(0, 0, 1920, 1080),
            new PixelSize(352, 360));

        Assert.Equal(new PixelPoint(1552, 704), position);
    }

    [Fact]
    public void Clamps_an_oversized_menu_to_the_working_area_origin()
    {
        var position = TrayMenuPlacementPolicy.GetPosition(
            new PixelRect(1920, 0, 800, 600),
            new PixelSize(1000, 900));

        Assert.Equal(new PixelPoint(1920, 0), position);
    }
}
