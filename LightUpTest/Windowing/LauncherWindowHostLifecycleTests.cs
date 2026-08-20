using LightUpUI.Models.Tiles;
using LightUpUI.Services;
using LightUpUI.ViewModels;

namespace LightUpTest.Windowing;

public sealed class LauncherWindowHostLifecycleTests
{
    [Fact]
    public void Search_window_host_is_safe_before_a_window_is_attached()
    {
        var host = new LauncherWindowHost();

        var exception = Record.Exception(() =>
        {
            host.Show();
            host.Toggle();
            host.Hide();
        });

        Assert.Null(exception);
        Assert.False(host.IsVisible);
    }

    [Fact]
    public void Tile_window_host_is_safe_before_a_window_is_attached()
    {
        var viewModel = new TileLauncherViewModel(
            new FakeStateStore(),
            new FakeProcessLauncher());
        var host = new TileLauncherWindowHost(viewModel);

        var exception = Record.Exception(() =>
        {
            host.Show();
            host.Toggle();
            host.Hide();
        });

        Assert.Null(exception);
        Assert.False(host.IsVisible);
    }

    private sealed class FakeStateStore : ILauncherStateStore
    {
        public Task<TileLauncherState> LoadAsync(CancellationToken cancellationToken)
            => Task.FromResult(new TileLauncherState());

        public Task SaveAsync(TileLauncherState state, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeProcessLauncher : IProcessLauncher
    {
        public Task<LaunchResult> LaunchAsync(
            LightUpUI.Models.LauncherItem item,
            CancellationToken cancellationToken)
            => Task.FromResult(LaunchResult.Success);
    }
}
