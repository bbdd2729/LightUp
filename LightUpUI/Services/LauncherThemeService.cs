using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using LightUpUI.Models;

namespace LightUpUI.Services;

public static class LauncherThemeService
{
    private static Application? _application;
    private static LauncherAppearanceSettings? _appearance;

    public static void Apply(LauncherAppearanceSettings appearance)
    {
        if (Application.Current is not { } application)
            return;

        if (!ReferenceEquals(_application, application))
        {
            if (_application is not null)
                _application.ActualThemeVariantChanged -= Application_ActualThemeVariantChanged;

            _application = application;
            application.ActualThemeVariantChanged += Application_ActualThemeVariantChanged;
        }

        _appearance = appearance;
        var mode = ThemePalettePolicy.NormalizeThemeMode(appearance.ThemeMode);
        application.RequestedThemeVariant = mode switch
        {
            LauncherThemeMode.Light => ThemeVariant.Light,
            LauncherThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };

        ApplyResources(application, appearance, mode);
    }

    private static void Application_ActualThemeVariantChanged(object? sender, EventArgs e)
    {
        if (_application is { } application && _appearance is { } appearance)
            ApplyResources(application, appearance, ThemePalettePolicy.NormalizeThemeMode(appearance.ThemeMode));
    }

    private static void ApplyResources(
        Application application,
        LauncherAppearanceSettings appearance,
        LauncherThemeMode mode)
    {
        var accent = ThemePalettePolicy.GetAccentColor(
            appearance.ColorPalette,
            appearance.CustomAccentColor);
        var isLight = ThemePalettePolicy.UsesLightResources(mode, application.ActualThemeVariant);

        SetBrush(application, "LightUpWindowBrush", isLight ? "F7F9FC" : "F0141A27");
        SetBrush(application, "LightUpSurfaceBrush", isLight ? "12000000" : "1AFFFFFF");
        SetBrush(application, "LightUpSurfaceStrongBrush", isLight ? "1E000000" : "24FFFFFF");
        SetBrush(application, "LightUpInputBrush", isLight ? "0D17212B" : "26FFFFFF");
        SetBrush(application, "LightUpInputHoverBrush", isLight ? "1417212B" : "32FFFFFF");
        SetBrush(application, "LightUpBorderBrush", isLight ? "24000000" : "32FFFFFF");
        SetBrush(application, "LightUpBorderBrushStrong", isLight ? "45000000" : "5AFFFFFF");
        SetBrush(application, "LightUpTextBrush", isLight ? "FF17212B" : "FFF7FAFC");
        SetBrush(application, "LightUpTextMutedBrush", isLight ? "FF52606D" : "A8C1CBD6");
        SetBrush(application, "LightUpTextFaintBrush", isLight ? "FF718096" : "7596A6B5");
        SetBrush(application, "LightUpAccentBrush", accent);
        SetBrush(application, "LightUpAccentForegroundBrush", GetAccentForeground(accent));
        SetBrush(application, "LightUpAccentSoftBrush", WithAlpha(accent, 0x30));
        SetBrush(application, "LightUpSelectionBrush", WithAlpha(accent, 0x3A));
        SetBrush(application, "LightUpHoverBrush", isLight ? "14000000" : "20FFFFFF");
        SetBrush(application, "LightUpFocusBrush", accent);
        SetBrush(application, "LightUpDisabledBrush", isLight ? "8052606D" : "5CC1CBD6");
        SetBrush(application, "LightUpDangerBrush", isLight ? "FFC6284A" : "FFE66D75");
        SetBrush(application, "LightUpDangerSoftBrush", isLight ? "1FC6284A" : "20E66D75");
        SetBrush(application, "LightUpSuccessBrush", isLight ? "FF0E8F5A" : "FF76E3A1");
        SetBrush(application, "LightUpWarningBrush", isLight ? "FFC27A00" : "FFFFD166");
    }

    private static void SetBrush(Application application, string key, string color)
        => application.Resources[key] = new SolidColorBrush(Color.Parse(
            color.StartsWith('#') ? color : $"#{color}"));

    private static void SetBrush(Application application, string key, Color color)
        => application.Resources[key] = new SolidColorBrush(color);

    private static string GetAccentForeground(Color accent)
    {
        var luminance = (0.299 * accent.R + 0.587 * accent.G + 0.114 * accent.B) / 255;
        return luminance > 0.64 ? "#FF17212B" : "#FFFFFFFF";
    }

    private static Color WithAlpha(Color color, byte alpha)
        => Color.FromArgb(alpha, color.R, color.G, color.B);
}
