using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;
using LightUpUI.Plugins;
using LightUpUI.Services;

namespace LightUpTest.Search;

public sealed class SearchPluginHostTests
{
    [Fact]
    public void Enabled_plugins_are_exposed_as_search_providers()
    {
        var host = new SearchPluginHost(
            [new FakePlugin("enabled", [CreateItem("Plugin app")])],
            new SearchLauncherSettings
            {
                Plugins = new Dictionary<string, PluginSettings>
                {
                    ["enabled"] = new() { IsEnabled = true }
                }
            });

        Assert.Single(host.Providers);
    }

    [Fact]
    public void Disabled_plugins_are_not_exposed()
    {
        var host = new SearchPluginHost(
            [new FakePlugin("disabled", [CreateItem("Hidden app")])],
            new SearchLauncherSettings
            {
                Plugins = new Dictionary<string, PluginSettings>
                {
                    ["disabled"] = new() { IsEnabled = false }
                }
            });

        Assert.Empty(host.Providers);
    }

    [Fact]
    public async Task Enabled_plugins_apply_their_configured_search_weight()
    {
        var host = new SearchPluginHost(
            [new FakePlugin("weighted", [CreateItem("Weighted app")])],
            new SearchLauncherSettings
            {
                Plugins = new Dictionary<string, PluginSettings>
                {
                    ["weighted"] = new() { IsEnabled = true, Weight = 75 }
                }
            });

        var results = await Assert.Single(host.Providers).SearchAsync("weighted", CancellationToken.None);

        Assert.Equal(75, Assert.Single(results).Relevance);
    }

    private static LauncherItem CreateItem(string title) => new(
        title.ToLowerInvariant().Replace(' ', '-'), title, "plugin", title, null, LauncherItemKind.Application);

    private sealed class FakePlugin(string id, IReadOnlyList<LauncherItem> items) : ISearchProviderPlugin
    {
        public string Id { get; } = id;

        public ISearchProvider CreateProvider() => new FakeProvider(items);
    }

    private sealed class FakeProvider(IReadOnlyList<LauncherItem> items) : ISearchProvider
    {
        public Task<IReadOnlyList<LauncherItem>> SearchAsync(string query, CancellationToken cancellationToken)
            => Task.FromResult(items);
    }
}
