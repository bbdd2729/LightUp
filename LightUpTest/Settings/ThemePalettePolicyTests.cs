using LightUpUI.Models;
using Avalonia.Styling;

namespace LightUpTest.Settings;

public sealed class ThemePalettePolicyTests
{
    [Fact]
    public void Normalize_invalid_theme_values_to_dark_defaults()
    {
        Assert.Equal(LauncherThemeMode.Dark, ThemePalettePolicy.NormalizeThemeMode((LauncherThemeMode)99));
        Assert.Equal(LauncherColorPalette.Ocean, ThemePalettePolicy.NormalizeColorPalette((LauncherColorPalette)99));
    }

    [Fact]
    public void Normalize_custom_color_preserves_valid_values_and_repairs_invalid_values()
    {
        Assert.Equal("#12ABEF", ThemePalettePolicy.NormalizeCustomAccentColor("#12ABEF"));
        Assert.Equal(ThemePalettePolicy.DefaultCustomAccentColor, ThemePalettePolicy.NormalizeCustomAccentColor("not-a-color"));
    }

    [Fact]
    public void Every_preset_palette_produces_a_visible_accent_color()
    {
        foreach (var palette in Enum.GetValues<LauncherColorPalette>())
        {
            var color = ThemePalettePolicy.GetAccentColor(palette, "#123456");
            Assert.True(color.A > 0);
        }
    }

    [Theory]
    [InlineData(LauncherThemeMode.Light, "Dark", true)]
    [InlineData(LauncherThemeMode.Dark, "Light", false)]
    [InlineData(LauncherThemeMode.System, "Light", true)]
    [InlineData(LauncherThemeMode.System, "Dark", false)]
    public void Resource_brightness_tracks_the_selected_theme_mode(
        LauncherThemeMode mode,
        string actualThemeName,
        bool expected)
    {
        var actualTheme = actualThemeName == "Light" ? ThemeVariant.Light : ThemeVariant.Dark;

        Assert.Equal(expected, ThemePalettePolicy.UsesLightResources(mode, actualTheme));
    }
}
