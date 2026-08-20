using LightUpUI.Services;

namespace LightUpTest.Windowing;

public sealed class LauncherStartupPolicyTests
{
    [Fact]
    public void Startup_uses_the_persistent_tile_launcher_as_the_main_surface()
    {
        Assert.Equal(LauncherStartupSurface.TileLauncher, LauncherStartupPolicy.MainSurface);
        Assert.True(LauncherStartupPolicy.ShouldShowMainSurfaceOnStartup);
    }
}
