using System.IO;
using System.Text.RegularExpressions;

namespace LightUpTest.Windowing;

public sealed class SemiThemeResourceTests
{
    [Fact]
    public void App_uses_semi_as_the_only_explicit_base_theme()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "App.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("xmlns:semi=\"https://irihi.tech/semi\"", markup);
        Assert.Contains("<semi:SemiTheme Locale=\"zh-CN\" />", markup);
        Assert.DoesNotContain("<FluentTheme", markup);
    }

    [Fact]
    public void Semi_theme_package_matches_the_Avalonia_12_ui_branch()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "LightUpUI.csproj");
        var project = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("PackageReference Include=\"Semi.Avalonia\" Version=\"12.1.0.1\"", project);
        Assert.DoesNotContain("PackageReference Include=\"Avalonia.Themes.Fluent\"", project);
    }

    [Fact]
    public void LightUp_keeps_business_control_styles_over_the_Semi_templates()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "App.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("Button.launcher-primary", markup);
        Assert.Contains("TextBox.launcher-query", markup);
        Assert.Contains("ComboBox.launcher-field", markup);
        Assert.Contains("CheckBox.launcher-field", markup);
        Assert.Contains("ContextMenu.launcher-context-menu", markup);
        Assert.Contains("ListBoxItem:focus-visible", markup);
        Assert.Contains("ComboBoxItem:pointerover", markup);
        Assert.Contains("ComboBoxItem:selected", markup);
        Assert.Contains("ProgressBar", markup);
    }

    [Fact]
    public void Shared_button_styles_use_the_Semi_control_themes()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "App.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("<semi:SemiPopupAnimations />", markup);
        Assert.Contains("Value=\"{DynamicResource BorderlessButton}\"", markup);
        Assert.Contains("Value=\"{DynamicResource SolidButton}\"", markup);
        Assert.Contains("Value=\"{DynamicResource OutlineButton}\"", markup);
        Assert.Contains("SemiColorPrimaryPointerover", markup);
        Assert.Contains("SemiBorderRadiusSmall", markup);
        Assert.Contains("Value=\"{DynamicResource CardBorder}\"", markup);
        Assert.Contains("Value=\"{DynamicResource SemiColorFill1}\"", markup);
    }

    [Fact]
    public void Theme_service_updates_the_Semi_tokens_when_the_appearance_changes()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Services", "LauncherThemeService.cs");
        var source = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("ApplySemiTokens(application, accent, isLight)", source);
        Assert.Contains("SemiColorPrimary", source);
        Assert.Contains("SemiColorText0", source);
        Assert.Contains("SemiColorBackground0", source);
        Assert.Contains("SemiColorFocusBorder", source);
        Assert.Contains("Scale(accent", source);
        Assert.Contains("SemiColorDisabledText", source);
        Assert.Contains("SemiColorDisabledBorder", source);
    }

    [Fact]
    public void Context_menus_have_a_bounded_scrollable_height()
    {
        var mainPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "MainWindow.axaml");
        var tilePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "TileLauncherWindow.axaml");

        var mainMarkup = File.ReadAllText(Path.GetFullPath(mainPath));
        var tileMarkup = File.ReadAllText(Path.GetFullPath(tilePath));

        Assert.Contains("MaxHeight=\"560\"", mainMarkup);
        Assert.Contains("ScrollViewer.VerticalScrollBarVisibility=\"Auto\"", mainMarkup);
        Assert.Contains("MaxHeight=\"560\"", tileMarkup);
        Assert.Contains("ScrollViewer.VerticalScrollBarVisibility=\"Auto\"", tileMarkup);
    }

    [Fact]
    public void Tile_editor_text_boxes_use_the_shared_Semi_compatible_field_style()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "TileLauncherWindow.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("x:Name=\"NewCategoryBox\"", markup);
        Assert.Contains("x:Name=\"CategoryNameBox\"", markup);
        Assert.Contains("x:Name=\"TileTitleBox\"", markup);
        Assert.Contains("x:Name=\"TileNotesBox\"", markup);
        Assert.Equal(0, CountFieldOverrides(markup, "Background=\"{DynamicResource LightUpWindowBrush}\""));
    }

    [Fact]
    public void Settings_controls_have_a_stable_unique_tab_order()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "SettingsWindow.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));
        var indexes = Regex.Matches(markup, "KeyboardNavigation\\.TabIndex=\\\"(?<index>\\d+)\\\"")
            .Select(match => int.Parse(match.Groups["index"].Value))
            .ToArray();

        Assert.Equal(Enumerable.Range(0, 18), indexes);
    }

    [Fact]
    public void Search_boxes_do_not_override_the_shared_query_theme()
    {
        var mainPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "MainWindow.axaml");
        var tilePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "TileLauncherWindow.axaml");
        var mainMarkup = File.ReadAllText(Path.GetFullPath(mainPath));
        var tileMarkup = File.ReadAllText(Path.GetFullPath(tilePath));
        var mainQuery = Regex.Match(mainMarkup, "<TextBox[^>]*x:Name=\"QueryBox\"[^>]*/>", RegexOptions.Singleline).Value;
        var tileQuery = Regex.Match(tileMarkup, "<TextBox[^>]*x:Name=\"SearchBox\"[^>]*/>", RegexOptions.Singleline).Value;

        Assert.NotEmpty(mainQuery);
        Assert.NotEmpty(tileQuery);
        Assert.DoesNotContain("Background=", mainQuery);
        Assert.DoesNotContain("Foreground=", mainQuery);
        Assert.DoesNotContain("Background=", tileQuery);
        Assert.DoesNotContain("Foreground=", tileQuery);
        Assert.Contains("TextBox.launcher-query", File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "App.axaml"))));
    }

    [Fact]
    public void Window_lists_preserve_selected_state_when_pointer_is_over_the_item()
    {
        var mainPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "MainWindow.axaml");
        var tilePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "TileLauncherWindow.axaml");
        var mainMarkup = File.ReadAllText(Path.GetFullPath(mainPath));
        var tileMarkup = File.ReadAllText(Path.GetFullPath(tilePath));

        Assert.Contains("ListBoxItem:selected:pointerover Border.result-row", mainMarkup);
        Assert.Contains("ListBoxItem:selected:pointerover Border.category-item", tileMarkup);
        Assert.Contains("ListBoxItem:selected:pointerover Border.tile-card", tileMarkup);
    }

    [Fact]
    public void Tile_launcher_business_containers_use_shared_Semi_tokens()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "TileLauncherWindow.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("Classes=\"launcher-panel\"", markup);
        Assert.Contains("Classes=\"launcher-chip\"", markup);
        Assert.Contains("Classes=\"launcher-empty-state\"", markup);
        Assert.Contains("CornerRadius=\"{DynamicResource SemiBorderRadiusSmall}\"", markup);
        Assert.Contains("SemiColorPrimaryLightPointerover", markup);
        Assert.DoesNotContain("Background=\"{DynamicResource LightUpSurfaceStrongBrush}\"", markup);
    }

    [Fact]
    public void Shared_disabled_states_use_Semi_disabled_tokens()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "App.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("SemiColorDisabledText", markup);
        Assert.Contains("SemiColorDisabledBorder", markup);
        Assert.Contains("SemiColorDisabledBackground", markup);
    }

    [Fact]
    public void Tray_menu_scroll_region_has_a_stable_height_boundary()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "TrayMenuWindow.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("MaxHeight=\"520\"", markup);
        Assert.Contains("Padding=\"0,2,4,2\"", markup);
        Assert.Contains("Text=\"退出 LightUp\"", markup);
    }

    private static int CountFieldOverrides(string markup, string value)
        => markup.Split(value, StringSplitOptions.None).Length - 1;
}
