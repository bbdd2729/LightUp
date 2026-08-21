using LightUpUI.Models;
using LightUpUI.Services;
using LightUpUI.ViewModels;

namespace LightUpTest.Launcher;

public sealed class MainViewModelResultActionTests
{
    [Fact]
    public async Task RevealSelectedItemAsync_reveals_file_backed_results_without_hiding_the_search_window()
    {
        var revealService = new FakePathRevealService(LaunchResult.Success);
        var windowHost = new FakeWindowHost();
        var item = new LauncherItem("shortcut:test", "Test", "", "C:\\Tools\\test.lnk", null, LauncherItemKind.Shortcut);
        var viewModel = CreateViewModel(windowHost, revealService);
        viewModel.SelectedItem = item;

        await viewModel.RevealSelectedItemAsync(TestContext.Current.CancellationToken);

        Assert.Equal(item.LaunchPath, revealService.LastPath);
        Assert.False(windowHost.WasHidden);
        Assert.Contains("已打开所在位置", viewModel.StatusText);
        Assert.False(viewModel.IsSearching);
    }

    [Fact]
    public async Task RevealSelectedItemAsync_rejects_built_in_actions_without_calling_the_reveal_service()
    {
        var revealService = new FakePathRevealService(LaunchResult.Success);
        var viewModel = CreateViewModel(new FakeWindowHost(), revealService);
        viewModel.SelectedItem = new LauncherItem(
            "action:settings", "Settings", "", "lightup:settings", null, LauncherItemKind.Action);

        await viewModel.RevealSelectedItemAsync(TestContext.Current.CancellationToken);

        Assert.Null(revealService.LastPath);
        Assert.Contains("没有可打开的位置", viewModel.StatusText);
    }

    private static MainViewModel CreateViewModel(ILauncherWindowHost windowHost, IPathRevealService revealService)
        => new(new EmptySearchService(), new FakeProcessLauncher(), windowHost, revealService);

    private sealed class EmptySearchService : ISearchService
    {
        public Task<IReadOnlyList<LauncherItem>> SearchAsync(string query, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<LauncherItem>>([]);

        public Task<IReadOnlyList<LauncherItem>> SearchAsync(
            SearchLauncherMode mode,
            string query,
            CancellationToken cancellationToken)
            => SearchAsync(query, cancellationToken);
    }

    private sealed class FakeProcessLauncher : IProcessLauncher
    {
        public Task<LaunchResult> LaunchAsync(LauncherItem item, CancellationToken cancellationToken)
            => Task.FromResult(LaunchResult.Success);
    }

    private sealed class FakeWindowHost : ILauncherWindowHost
    {
        public bool IsVisible => true;
        public bool WasHidden { get; private set; }
        public void Toggle() { }
        public void Show() { }
        public void Hide() => WasHidden = true;
    }

    private sealed class FakePathRevealService(LaunchResult result) : IPathRevealService
    {
        public string? LastPath { get; private set; }

        public Task<LaunchResult> RevealAsync(string path, CancellationToken cancellationToken)
        {
            LastPath = path;
            return Task.FromResult(result);
        }
    }
}
