using System.Diagnostics;
using LightUpUI.Services;

namespace LightUpTest.Launcher;

public sealed class WindowsEverythingLauncherTests
{
    [Fact]
    public async Task OpenSearchAsync_starts_the_resolved_executable_with_a_search_argument()
    {
        ProcessStartInfo? receivedStartInfo = null;
        var launcher = new WindowsEverythingLauncher(
            new FakeExecutableLocator("C:\\Tools\\Everything\\Everything.exe"),
            startInfo =>
            {
                receivedStartInfo = startInfo;
                return Process.GetCurrentProcess();
            });

        var result = await launcher.OpenSearchAsync("project report", TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(receivedStartInfo);
        Assert.Equal("C:\\Tools\\Everything\\Everything.exe", receivedStartInfo.FileName);
        Assert.Equal(["-search", "project report"], receivedStartInfo.ArgumentList);
    }

    [Fact]
    public async Task OpenSearchAsync_reports_when_everything_is_not_installed()
    {
        var wasStarted = false;
        var launcher = new WindowsEverythingLauncher(
            new FakeExecutableLocator(null),
            _ =>
            {
                wasStarted = true;
                return Process.GetCurrentProcess();
            });

        var result = await launcher.OpenSearchAsync("report", TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("Everything.exe", result.ErrorMessage);
        Assert.False(wasStarted);
    }

    private sealed class FakeExecutableLocator(string? path) : IEverythingExecutableLocator
    {
        public string? FindExecutablePath() => path;
    }
}
