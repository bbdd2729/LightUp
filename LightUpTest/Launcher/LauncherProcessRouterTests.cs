using LightUpUI.Models;
using LightUpUI.Services;

namespace LightUpTest.Launcher;

public sealed class LauncherProcessRouterTests
{
    [Fact]
    public async Task Action_items_are_sent_to_the_action_handler()
    {
        var routed = false;
        var router = new LauncherProcessRouter(
            new FakeProcessLauncher(LaunchResult.Failed("should not be called")),
            (_, _) =>
            {
                routed = true;
                return Task.FromResult(LaunchResult.Success);
            });

        var result = await router.LaunchAsync(
            new LauncherItem("action:tiles", "Tiles", "", "lightup:tiles", null, LauncherItemKind.Action),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.True(routed);
    }

    private sealed class FakeProcessLauncher(LaunchResult result) : IProcessLauncher
    {
        public Task<LaunchResult> LaunchAsync(LauncherItem item, CancellationToken cancellationToken)
            => Task.FromResult(result);
    }
}
