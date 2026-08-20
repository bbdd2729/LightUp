using LightUpUI.Models;
using LightUpUI.Presentation;

namespace LightUpTest.Presentation;

public sealed class LauncherItemVisualsTests
{
    [Theory]
    [InlineData(LauncherItemKind.Application)]
    [InlineData(LauncherItemKind.Shortcut)]
    [InlineData(LauncherItemKind.PathExecutable)]
    [InlineData(LauncherItemKind.Action)]
    public void Kind_gets_a_fluent_vector_icon(LauncherItemKind kind)
    {
        var icon = LauncherItemVisuals.GetIcon(kind);

        Assert.NotEqual(default, icon);
    }

    [Fact]
    public void Every_supported_kind_has_a_distinct_icon_geometry()
    {
        var iconData = Enum.GetValues<LauncherItemKind>()
            .Select(LauncherItemVisuals.GetIcon)
            .ToArray();

        Assert.Equal(iconData.Length, new HashSet<FluentIcons.Common.Icon>(iconData).Count);
    }

    [Theory]
    [InlineData(LauncherItemKind.Application, "应用")]
    [InlineData(LauncherItemKind.Shortcut, "快捷方式")]
    [InlineData(LauncherItemKind.PathExecutable, "系统程序")]
    [InlineData(LauncherItemKind.Action, "功能")]
    public void Kind_gets_a_localized_label(LauncherItemKind kind, string expectedLabel)
    {
        Assert.Equal(expectedLabel, LauncherItemVisuals.GetLabel(kind));
    }
}
