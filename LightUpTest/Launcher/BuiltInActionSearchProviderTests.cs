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
    }

    [Fact]
    public async Task Query_is_carried_to_the_everything_action()
    {
        var provider = new BuiltInActionSearchProvider();

        var results = await provider.SearchAsync("report", TestContext.Current.CancellationToken);

        var everything = Assert.Single(results, item => item.Id == "action:everything");
        Assert.Equal("report", everything.Arguments);
    }
}
