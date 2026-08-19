using LightUpUI.Models;
using LightUpUI.Services;

namespace LightUpTest.Launcher;

public sealed class SearchServiceTests
{
    [Fact]
    public async Task SearchAsync_filters_results_by_title_and_path()
    {
        var service = new SearchService(
        [
            new FakeProvider(
            [
                new LauncherItem("notepad", "Notepad", "Windows", "C:\\Windows\\notepad.exe", null, LauncherItemKind.PathExecutable),
                new LauncherItem("calc", "Calculator", "Windows", "C:\\Windows\\calc.exe", null, LauncherItemKind.PathExecutable)
            ])
        ]);

        var results = await service.SearchAsync("note", CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("notepad", results[0].Id);
    }

    [Fact]
    public async Task SearchAsync_deduplicates_by_id_and_orders_by_relevance_then_title()
    {
        var service = new SearchService(
        [
            new FakeProvider(
            [
                new LauncherItem("z", "Zeta", "", "z.exe", null, LauncherItemKind.PathExecutable),
                new LauncherItem("a", "Alpha", "", "a.exe", null, LauncherItemKind.Shortcut),
                new LauncherItem("z", "Zeta duplicate", "", "z.exe", null, LauncherItemKind.Shortcut)
            ])
        ]);

        var results = await service.SearchAsync("", CancellationToken.None);

        Assert.Equal(["a", "z"], results.Select(item => item.Id));
    }

    private sealed class FakeProvider(IReadOnlyList<LauncherItem> items) : ISearchProvider
    {
        public Task<IReadOnlyList<LauncherItem>> SearchAsync(string query, CancellationToken cancellationToken)
            => Task.FromResult(items);
    }
}
