using System.Diagnostics;
using LightUpUI.Models;
using LightUpUI.Services;

namespace LightUpTest.Services;

public sealed class WindowsAdministratorProcessLauncherTests
{
    [Fact]
    public async Task Supported_item_uses_the_runas_shell_verb()
    {
        ProcessStartInfo? captured = null;
        var launcher = new WindowsAdministratorProcessLauncher(startInfo =>
        {
            captured = startInfo;
            return null;
        });
        var item = new LauncherItem(
            "app:tool", "Tool", "", "C:\\Tools\\tool.exe", "--safe", LauncherItemKind.Application);

        var result = await launcher.LaunchAsAdministratorAsync(item, TestContext.Current.CancellationToken);

        if (OperatingSystem.IsWindows())
        {
            Assert.False(result.Succeeded);
            Assert.Equal("runas", captured?.Verb);
            Assert.Equal(item.LaunchPath, captured?.FileName);
            Assert.Equal(item.Arguments, captured?.Arguments);
        }
        else
        {
            Assert.False(result.Succeeded);
            Assert.Null(captured);
        }
    }

    [Fact]
    public async Task Unsupported_item_is_rejected_before_process_start()
    {
        var started = false;
        var launcher = new WindowsAdministratorProcessLauncher(_ =>
        {
            started = true;
            return null;
        });
        var item = new LauncherItem(
            "file:text", "Text", "", "C:\\Docs\\readme.txt", null, LauncherItemKind.File);

        var result = await launcher.LaunchAsAdministratorAsync(item, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.False(started);
    }
}
