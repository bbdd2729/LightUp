using LightUpUI.Models;
using LightUpUI.Services;

namespace LightUpTest.Launcher;

public sealed class LauncherActionHostTests
{
    [Theory]
    [InlineData("action:open-url", "https://example.com/docs", "https://example.com/docs")]
    [InlineData("action:web-search", "LightUp docs", "https://www.bing.com/search?q=LightUp%20docs")]
    [InlineData("action:windows-settings", null, "ms-settings:")]
    public async Task Uri_actions_are_delegated_to_the_uri_launcher(
        string actionId,
        string? arguments,
        string expectedUri)
    {
        var uriLauncher = new FakeUriLauncher(LaunchResult.Success);
        var host = CreateHost(uriLauncher);

        var result = await host.ExecuteAsync(
            new LauncherItem(actionId, "Action", "", "lightup:action", arguments, LauncherItemKind.Action),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(expectedUri, uriLauncher.LastUri);
    }

    [Fact]
    public async Task Uri_action_failure_is_returned_to_the_caller()
    {
        var uriLauncher = new FakeUriLauncher(LaunchResult.Failed("Browser unavailable"));
        var host = CreateHost(uriLauncher);

        var result = await host.ExecuteAsync(
            new LauncherItem("action:web-search", "Search", "", "lightup:web-search", "LightUp", LauncherItemKind.Action),
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal("Browser unavailable", result.ErrorMessage);
    }

    [Fact]
    public async Task Everything_action_is_delegated_to_the_everything_launcher()
    {
        var everythingLauncher = new FakeEverythingLauncher(LaunchResult.Success);
        var host = CreateHost(new FakeUriLauncher(LaunchResult.Success), everythingLauncher);

        var result = await host.ExecuteAsync(
            new LauncherItem("action:everything", "Everything", "", "lightup:everything", "report", LauncherItemKind.Action),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("report", everythingLauncher.LastQuery);
    }

    [Fact]
    public async Task Calculation_action_copies_the_formatted_result()
    {
        string? copiedText = null;
        var host = CreateHost(
            new FakeUriLauncher(LaunchResult.Success),
            copyText: value =>
            {
                copiedText = value;
                return Task.FromResult(LaunchResult.Success);
            });

        var result = await host.ExecuteAsync(
            new LauncherItem("action:copy-calculation", "2 + 2 = 4", "", "lightup:calculator", "4", LauncherItemKind.Action),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("4", copiedText);
    }

    private static LauncherActionHost CreateHost(
        IUriLauncher uriLauncher,
        IEverythingLauncher? everythingLauncher = null,
        Func<string, Task<LaunchResult>>? copyText = null)
        => new(new FakeTileLauncherWindowHost(), () => Task.CompletedTask, uriLauncher, everythingLauncher, copyText);

    private sealed class FakeUriLauncher(LaunchResult result) : IUriLauncher
    {
        public string? LastUri { get; private set; }

        public Task<LaunchResult> OpenAsync(string uri, CancellationToken cancellationToken)
        {
            LastUri = uri;
            return Task.FromResult(result);
        }
    }

    private sealed class FakeTileLauncherWindowHost : ITileLauncherWindowHost
    {
        public bool IsVisible => false;
        public void Toggle() { }
        public void Show() { }
        public void Hide() { }
    }

    private sealed class FakeEverythingLauncher(LaunchResult result) : IEverythingLauncher
    {
        public string? LastQuery { get; private set; }

        public Task<LaunchResult> OpenSearchAsync(string query, CancellationToken cancellationToken)
        {
            LastQuery = query;
            return Task.FromResult(result);
        }
    }
}
