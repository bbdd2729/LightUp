using LightUpUI.Models;
using LightUpUI.Services;

namespace LightUpTest.Launcher;

public sealed class SearchModeTests
{
    [Fact]
    public async Task Simple_mode_uses_only_the_simple_provider_set()
    {
        var simple = new FakeProvider("tile", "My Tile");
        var full = new FakeProvider("desktop", "Desktop Shortcut");
        var service = new SearchService([simple, full], [simple]);

        var results = await service.SearchAsync(SearchLauncherMode.Simple, "", CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("tile", results[0].Id);
    }

    [Fact]
    public async Task Full_mode_uses_all_registered_providers()
    {
        var simple = new FakeProvider("tile", "My Tile");
        var full = new FakeProvider("desktop", "Desktop Shortcut");
        var service = new SearchService([simple, full], [simple]);

        var results = await service.SearchAsync(SearchLauncherMode.Full, "", CancellationToken.None);

        Assert.Equal(["desktop", "tile"], results.Select(item => item.Id));
    }

    private sealed class FakeProvider(string id, string title) : ISearchProvider
    {
        public Task<IReadOnlyList<LauncherItem>> SearchAsync(string query, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<LauncherItem>>(
            [new LauncherItem(id, title, "", $"{id}.exe", null, LauncherItemKind.Shortcut)]);
    }
}
