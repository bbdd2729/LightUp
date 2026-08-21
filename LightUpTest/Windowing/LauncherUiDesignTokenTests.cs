using System.IO;

namespace LightUpTest.Windowing;

public sealed class LauncherUiDesignTokenTests
{
    [Fact]
    public void App_declares_shared_input_focus_and_secondary_control_styles()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "App.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("LightUpInputBrush", markup);
        Assert.Contains("LightUpInputHoverBrush", markup);
        Assert.Contains("LightUpFocusBrush", markup);
        Assert.Contains("TextBox.launcher-field", markup);
        Assert.Contains("ComboBox.launcher-field", markup);
        Assert.Contains("Button.launcher-secondary", markup);
        Assert.Contains("Border.launcher-settings-section", markup);
        Assert.Contains("ContextMenu.launcher-context-menu", markup);
    }

    [Fact]
    public void Settings_keep_the_save_bar_outside_the_scrollable_content()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "SettingsWindow.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("<ScrollViewer Grid.Row=\"1\"", markup);
        Assert.Contains("<Border Grid.Row=\"2\"", markup);
        Assert.Contains("Classes=\"launcher-settings-section\"", markup);
        Assert.Contains("Classes=\"launcher-field\"", markup);
        Assert.Contains("AutomationProperties.Name=\"保存设置\"", markup);
    }

    [Fact]
    public void Search_window_declares_stable_minimum_size_and_result_rows()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "MainWindow.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("MinWidth=\"520\"", markup);
        Assert.Contains("MinHeight=\"240\"", markup);
        Assert.Contains("MinHeight=\"58\"", markup);
        Assert.Contains("TextTrimming=\"CharacterEllipsis\"", markup);
    }

    [Fact]
    public void Tray_menu_scrolls_actions_without_moving_the_exit_action()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "TrayMenuWindow.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("<ScrollViewer Grid.Row=\"1\"", markup);
        Assert.Contains("<Button Grid.Row=\"2\" Classes=\"tray-menu-item tray-menu-exit\"", markup);
        Assert.Contains("MinHeight\" Value=\"56\"", File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "App.axaml"))));
    }
}
