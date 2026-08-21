using LightUpUI.Models;

namespace LightUpTest.Launcher;

public sealed class LauncherItemActionPolicyTests
{
    [Fact]
    public void File_backed_results_expose_reveal_and_copy_actions()
    {
        var item = new LauncherItem("shortcut:test", "Test", "", "C:\\Tools\\test.lnk", null, LauncherItemKind.Shortcut);

        Assert.True(item.CanRevealLocation);
        Assert.True(item.CanCopyLaunchPath);
    }

    [Fact]
    public void Built_in_actions_can_be_copied_but_cannot_be_revealed()
    {
        var item = new LauncherItem("action:settings", "Settings", "", "lightup:settings", null, LauncherItemKind.Action);

        Assert.False(item.CanRevealLocation);
        Assert.True(item.CanCopyLaunchPath);
    }
}
