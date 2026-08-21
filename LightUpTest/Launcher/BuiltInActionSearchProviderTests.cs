using LightUpUI.Models;
using LightUpUI.Services;

namespace LightUpTest.Launcher;

public sealed class BuiltInActionSearchProviderTests
{
    [Fact]
    public async Task Empty_query_exposes_core_full_mode_actions()
    {
        var provider = new BuiltInActionSearchProvider();

        var results = await provider.SearchAsync(string.Empty, TestContext.Current.CancellationToken);

        Assert.Contains(results, item => item.Id == "action:everything");
        Assert.Contains(results, item => item.Id == "action:tiles");
        Assert.Contains(results, item => item.Id == "action:settings");
        Assert.Contains(results, item => item.Id == "action:windows-settings");
    }

    [Fact]
    public async Task Query_is_carried_to_the_everything_action()
    {
        var provider = new BuiltInActionSearchProvider();

        var results = await provider.SearchAsync("report", TestContext.Current.CancellationToken);

        var everything = Assert.Single(results, item => item.Id == "action:everything");
        Assert.Equal("report", everything.Arguments);
    }

    [Fact]
    public async Task Http_query_exposes_a_direct_open_action_without_a_web_search_duplicate()
    {
        var provider = new BuiltInActionSearchProvider();

        var results = await provider.SearchAsync("https://example.com/docs", TestContext.Current.CancellationToken);

        var openUrl = Assert.Single(results, item => item.Id == "action:open-url");
        Assert.Equal("https://example.com/docs", openUrl.Arguments);
        Assert.DoesNotContain(results, item => item.Id == "action:web-search");
    }

    [Fact]
    public async Task Text_query_exposes_a_web_search_action()
    {
        var provider = new BuiltInActionSearchProvider();

        var results = await provider.SearchAsync("LightUp docs", TestContext.Current.CancellationToken);

        var webSearch = Assert.Single(results, item => item.Id == "action:web-search");
        Assert.Equal("LightUp docs", webSearch.Arguments);
    }

    [Theory]
    [InlineData("! report", "action:everything", "report")]
    [InlineData("? LightUp docs", "action:web-search", "LightUp docs")]
    public async Task Prefix_query_routes_to_its_single_explicit_action(
        string query,
        string expectedActionId,
        string expectedArguments)
    {
        var provider = new BuiltInActionSearchProvider();

        var result = Assert.Single(await provider.SearchAsync(query, TestContext.Current.CancellationToken));

        Assert.Equal(expectedActionId, result.Id);
        Assert.Equal(expectedArguments, result.Arguments);
        Assert.Contains(query, result.Title);
    }

    [Fact]
    public async Task Calculator_prefix_is_reserved_for_the_calculator_provider()
    {
        var provider = new BuiltInActionSearchProvider();

        var results = await provider.SearchAsync("= 2 + 2", TestContext.Current.CancellationToken);

        Assert.Empty(results);
    }
}
