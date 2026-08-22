using System.IO;

namespace LightUpTest.Windowing;

public sealed class SettingsWindowStyleTests
{
    [Fact]
    public void Settings_window_uses_lightup_surface_tokens_and_primary_save_action()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "SettingsWindow.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("Background=\"{DynamicResource LightUpWindowBrush}\"", markup);
        Assert.Contains("Foreground=\"{DynamicResource LightUpTextBrush}\"", markup);
        Assert.Contains("Classes=\"launcher-primary\"", markup);
        Assert.Contains("AutomationProperties.Name=\"保存设置\"", markup);
        Assert.Contains("AutomationProperties.Name=\"外观主题\"", markup);
        Assert.Contains("KeyboardNavigation.TabIndex=\"13\"", markup);
        Assert.Contains("IsDefault=\"True\"", markup);
        Assert.Contains("ScrollViewer.BringIntoViewOnFocusChange=\"True\"", markup);
        Assert.DoesNotContain("Background=\"White\"", markup);
    }
}
