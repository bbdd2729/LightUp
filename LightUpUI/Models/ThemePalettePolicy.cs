using System;
using Avalonia.Media;

namespace LightUpUI.Models;

public static class ThemePalettePolicy
{
    public const string DefaultCustomAccentColor = "#4CC2FF";

    public static LauncherThemeMode NormalizeThemeMode(LauncherThemeMode mode)
        => mode is LauncherThemeMode.System or LauncherThemeMode.Light or LauncherThemeMode.Dark
            ? mode
            : LauncherThemeMode.Dark;

    public static LauncherColorPalette NormalizeColorPalette(LauncherColorPalette palette)
        => palette is LauncherColorPalette.Ocean
            or LauncherColorPalette.Teal
            or LauncherColorPalette.Violet
            or LauncherColorPalette.Amber
            or LauncherColorPalette.Rose
            or LauncherColorPalette.Lime
            or LauncherColorPalette.Custom
            ? palette
            : LauncherColorPalette.Ocean;

    public static string NormalizeCustomAccentColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DefaultCustomAccentColor;

        try
        {
            _ = Color.Parse(value.Trim());
            return value.Trim();
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            return DefaultCustomAccentColor;
        }
    }

    public static Color GetAccentColor(LauncherColorPalette palette, string? customAccentColor)
    {
        if (NormalizeColorPalette(palette) == LauncherColorPalette.Custom)
        {
            try
            {
                return Color.Parse(NormalizeCustomAccentColor(customAccentColor));
            }
            catch (Exception exception) when (exception is FormatException or ArgumentException)
            {
                return Color.Parse(DefaultCustomAccentColor);
            }
        }

        return palette switch
        {
            LauncherColorPalette.Teal => Color.Parse("#14B8A6"),
            LauncherColorPalette.Violet => Color.Parse("#8B5CF6"),
            LauncherColorPalette.Amber => Color.Parse("#F59E0B"),
            LauncherColorPalette.Rose => Color.Parse("#F43F5E"),
            LauncherColorPalette.Lime => Color.Parse("#65A30D"),
            _ => Color.Parse("#3B82F6")
        };
    }
}
