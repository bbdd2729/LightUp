using System.IO;

namespace LightUpTest.Windowing;

public sealed class SettingsWindowStyleTests
{
    [Fact]
    public void Settings_window_uses_semi_surface_tokens_and_primary_save_action()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "SettingsWindow.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("Background=\"{DynamicResource SemiColorBackground0}\"", markup);
        Assert.Contains("Foreground=\"{DynamicResource SemiColorText0}\"", markup);
        Assert.Contains("Classes=\"launcher-primary\"", markup);
        Assert.Contains("AutomationProperties.Name=\"保存设置\"", markup);
        Assert.Contains("AutomationProperties.Name=\"外观主题\"", markup);
        Assert.Contains("KeyboardNavigation.TabIndex=\"17\"", markup);
        Assert.Contains("IsDefault=\"True\"", markup);
        Assert.Contains("ScrollViewer.BringIntoViewOnFocusChange=\"True\"", markup);
        Assert.DoesNotContain("Background=\"White\"", markup);
    }
}
