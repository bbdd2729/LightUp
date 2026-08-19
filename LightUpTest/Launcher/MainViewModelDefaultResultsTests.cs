using LightUpUI.Models;
using LightUpUI.Services;
using LightUpUI.ViewModels;

namespace LightUpTest.Launcher;

public sealed class MainViewModelDefaultResultsTests
{
    [Fact]
    public async Task Activating_the_search_launcher_loads_at_most_ten_default_results()
    {
        var searchService = new FakeSearchService(Enumerable.Range(1, 12)
            .Select(index => new LauncherItem($"item:{index}", $"Item {index}", "", $"item{index}.exe", null, LauncherItemKind.Shortcut))
            .ToArray());
        var viewModel = new MainViewModel(searchService, new FakeProcessLauncher(), new FakeWindowHost());

        viewModel.ResetForActivation();
        await Task.Yield();

        Assert.Equal(10, viewModel.Results.Count);
        Assert.Equal(string.Empty, searchService.LastQuery);
    }

    private sealed class FakeSearchService(IReadOnlyList<LauncherItem> results) : ISearchService
    {
        public string? LastQuery { get; private set; }

        public Task<IReadOnlyList<LauncherItem>> SearchAsync(string query, CancellationToken cancellationToken)
        {
            LastQuery = query;
            return Task.FromResult(results);
        }

        public Task<IReadOnlyList<LauncherItem>> SearchAsync(SearchLauncherMode mode, string query, CancellationToken cancellationToken)
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
        public void Toggle() { }
        public void Show() { }
        public void Hide() { }
    }
}
