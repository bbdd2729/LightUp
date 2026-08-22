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
        Assert.Contains("Button.launcher-chrome.is-active", markup);
        Assert.Contains("Button.launcher-tile-remove:pointerover", markup);
        Assert.Contains("Border.tile-drag-handle:pointerover", markup);
        Assert.Contains("Border.tile-drag-handle:pressed", markup);
        Assert.Contains("Border.tile-drag-handle:focus-visible", markup);
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
        Assert.Contains("AutomationProperties.Name=\"左键点击托盘图标动作\"", markup);
        Assert.Contains("TrayIconLeftClickActionLabelConverter", markup);
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
        Assert.Contains("Classes=\"launcher-empty-state\"", markup);
        Assert.Contains("Classes=\"launcher-status-bar\"", markup);
        Assert.Contains("ShowEmptyState", markup);
        Assert.Contains("ShowStatusBar", markup);
        Assert.Contains("AutomationProperties.Name=\"{CompiledBinding Title}\"", markup);
        Assert.Contains("AutomationProperties.HelpText=\"{CompiledBinding Subtitle}\"", markup);
        Assert.Contains("IsEnabled=\"{Binding HasQueryText}\"", markup);
        Assert.Contains("ScrollViewer.BringIntoViewOnFocusChange=\"True\"", markup);
        Assert.Contains("AutomationProperties.Name=\"切换搜索窗口置顶\"", markup);
    }

    [Fact]
    public void Tile_drag_overlay_uses_theme_resources()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "TileLauncherWindow.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("LightUpDropBrush", markup);
        Assert.Contains("LightUpDangerSoftBrush", markup);
        Assert.DoesNotContain("#D91B2A3D", markup);
    }

    [Fact]
    public void Tile_confirmation_and_footer_hints_have_stable_text_overflow_rules()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "TileLauncherWindow.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("TextWrapping=\"Wrap\"", markup);
        Assert.Contains("MaxLines=\"2\"", markup);
        Assert.Contains("TextTrimming=\"CharacterEllipsis\"", markup);
        Assert.Contains("Text=\"单击选择 · 双击打开 · Enter 打开 · Esc 收起\"", markup);
        Assert.Contains("Text=\"拖放文件或 HTTP(S) 地址也可添加入口\"", markup);
    }

    [Fact]
    public void Tray_menu_scrolls_actions_without_moving_the_exit_action()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "TrayMenuWindow.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("<ScrollViewer Grid.Row=\"1\"", markup);
        Assert.Contains("<Button Grid.Row=\"2\" Classes=\"tray-menu-item tray-menu-exit\"", markup);
        Assert.Contains("MinHeight\" Value=\"56\"", File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "App.axaml"))));
        Assert.Contains("ColumnDefinitions=\"30,*,92\"", markup);
        Assert.Contains("AutomationProperties.Name=\"退出 LightUp\"", markup);
        Assert.Contains("KeyboardNavigation.TabIndex=\"3\"", markup);
    }

    [Fact]
    public void Tray_icon_uses_a_native_right_click_menu_and_a_configurable_left_click_action()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "App.axaml.cs");
        var source = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("trayIcon.Menu = CreateTrayNativeMenu", source);
        Assert.Contains("ToolTipText = \"LightUp 启动器 · 左键打开入口，右键打开菜单\"", source);
        Assert.Contains("trayIcon.Clicked", source);
        Assert.Contains("getLeftClickAction() == TrayIconLeftClickAction.Tiles", source);
        Assert.DoesNotContain("quickMenu", source);
    }
}
