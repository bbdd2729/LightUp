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

    [Fact]
    public async Task Tile_window_host_starts_one_shared_async_load_without_a_window()
    {
        var stateStore = new DeferredStateStore();
        var viewModel = new TileLauncherViewModel(stateStore, new FakeProcessLauncher());
        var host = new TileLauncherWindowHost(viewModel);

        var initialLoad = host.EnsureLoadedAsync();
        var repeatedLoad = host.EnsureLoadedAsync();

        Assert.Same(initialLoad, repeatedLoad);
        Assert.Equal(1, stateStore.LoadCount);
        Assert.False(initialLoad.IsCompleted);

        stateStore.Complete(new TileLauncherState());
        await initialLoad;
    }

    private sealed class FakeStateStore : ILauncherStateStore
    {
        public Task<TileLauncherState> LoadAsync(CancellationToken cancellationToken)
            => Task.FromResult(new TileLauncherState());

        public Task SaveAsync(TileLauncherState state, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class DeferredStateStore : ILauncherStateStore
    {
        private readonly TaskCompletionSource<TileLauncherState> _stateSource = new();

        public int LoadCount { get; private set; }

        public Task<TileLauncherState> LoadAsync(CancellationToken cancellationToken)
        {
            LoadCount++;
            return _stateSource.Task;
        }

        public Task SaveAsync(TileLauncherState state, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public void Complete(TileLauncherState state) => _stateSource.SetResult(state);
    }

    private sealed class FakeProcessLauncher : IProcessLauncher
    {
        public Task<LaunchResult> LaunchAsync(
            LightUpUI.Models.LauncherItem item,
            CancellationToken cancellationToken)
            => Task.FromResult(LaunchResult.Success);
    }
}
