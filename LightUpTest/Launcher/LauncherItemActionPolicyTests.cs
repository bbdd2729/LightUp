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

    [Theory]
    [InlineData("action:copy-calculation", true)]
    [InlineData("action:web-search", false)]
    [InlineData("shortcut:notepad", false)]
    public void Successful_actions_only_keep_the_search_open_when_their_feedback_must_remain_visible(
        string id,
        bool expected)
    {
        var item = new LauncherItem(id, "Item", "", "path", null, LauncherItemKind.Action);

        Assert.Equal(expected, LauncherItemActionPolicy.ShouldKeepSearchOpenAfterSuccess(item));
    }

    [Fact]
    public void Search_query_action_requires_a_non_empty_query_argument()
    {
        var valid = new LauncherItem(
            "action:search-query:docs", "Search", "", "lightup:search-query", "docs", LauncherItemKind.Action);
        var invalid = valid with { Arguments = " " };

        Assert.True(LauncherItemActionPolicy.IsSearchQueryAction(valid));
        Assert.False(LauncherItemActionPolicy.IsSearchQueryAction(invalid));
    }

    [Theory]
    [InlineData("C:\\Tools\\tool.exe", true)]
    [InlineData("C:\\Tools\\tool.lnk", true)]
    [InlineData("C:\\Docs\\readme.txt", false)]
    public void Administrator_action_is_limited_to_elevatable_file_types(string path, bool expected)
    {
        var item = new LauncherItem("item", "Item", "", path, null, LauncherItemKind.File);

        Assert.Equal(expected, LauncherItemActionPolicy.CanRunAsAdministrator(item));
    }
}
