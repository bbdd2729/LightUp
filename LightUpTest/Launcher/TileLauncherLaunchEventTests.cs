using LightUpUI.Models;
using LightUpUI.Models.Tiles;
using LightUpUI.Services;
using LightUpUI.ViewModels;

namespace LightUpTest.Launcher;

public sealed class TileLauncherLaunchEventTests
{
    [Fact]
    public async Task OpenSelectedCommand_raises_LaunchSucceeded_after_a_successful_launch()
    {
        var viewModel = await CreateViewModelAsync(LaunchResult.Success);
        var eventCount = 0;
        viewModel.LaunchSucceeded += (_, _) => eventCount++;

        await viewModel.OpenSelectedCommand.ExecuteAsync(null);

        Assert.Equal(1, eventCount);
    }

    [Fact]
    public async Task OpenSelectedCommand_does_not_raise_LaunchSucceeded_after_a_failed_launch()
    {
        var viewModel = await CreateViewModelAsync(LaunchResult.Failed("启动失败"));
        var eventCount = 0;
        viewModel.LaunchSucceeded += (_, _) => eventCount++;

        await viewModel.OpenSelectedCommand.ExecuteAsync(null);

        Assert.Equal(0, eventCount);
    }

    private static async Task<TileLauncherViewModel> CreateViewModelAsync(LaunchResult result)
    {
        var item = new TileItem
        {
            Id = "tool",
            Title = "工具",
            TargetPath = "tool.exe"
        };
        var viewModel = new TileLauncherViewModel(
            new FakeStateStore(new TileLauncherState
            {
                SelectedCategoryId = "all",
                Categories = [new TileCategory { Id = "all", Name = "全部", Items = [item] }]
            }),
            new FixedProcessLauncher(result),
            targetHealthService: new AvailableTargetHealthService());
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SelectedItem = item;
        return viewModel;
    }

    private sealed class FakeStateStore(TileLauncherState state) : ILauncherStateStore
    {
        public Task<TileLauncherState> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(state);
        public Task SaveAsync(TileLauncherState state, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FixedProcessLauncher(LaunchResult result) : IProcessLauncher
    {
        public Task<LaunchResult> LaunchAsync(LauncherItem item, CancellationToken cancellationToken)
            => Task.FromResult(result);
    }

    private sealed class AvailableTargetHealthService : ITileTargetHealthService
    {
        public TileTargetHealthResult Evaluate(TileItem item) => TileTargetHealthResult.Available;
    }
}
