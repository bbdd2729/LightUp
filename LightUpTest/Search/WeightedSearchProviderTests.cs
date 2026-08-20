using LightUpUI.Models;
using LightUpUI.Services;

namespace LightUpTest.Search;

public sealed class WeightedSearchProviderTests
{
    [Fact]
    public async Task SearchAsync_adds_the_normalized_plugin_weight_to_each_result()
    {
        var provider = new WeightedSearchProvider(
            new FakeProvider([new LauncherItem("plugin", "Plugin result", "", "plugin.exe", null, LauncherItemKind.Application, Relevance: 20)]),
            weight: 50);

        var results = await provider.SearchAsync("plugin", CancellationToken.None);

        Assert.Equal(70, Assert.Single(results).Relevance);
    }

    [Fact]
    public async Task SearchAsync_clamps_an_out_of_range_plugin_weight()
    {
        var provider = new WeightedSearchProvider(
            new FakeProvider([new LauncherItem("plugin", "Plugin result", "", "plugin.exe", null, LauncherItemKind.Application)]),
            weight: 2000);

        var results = await provider.SearchAsync("plugin", CancellationToken.None);

        Assert.Equal(1000, Assert.Single(results).Relevance);
    }

    private sealed class FakeProvider(IReadOnlyList<LauncherItem> items) : ISearchProvider
    {
        public Task<IReadOnlyList<LauncherItem>> SearchAsync(string query, CancellationToken cancellationToken)
            => Task.FromResult(items);
    }
}
