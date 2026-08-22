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
        SetBrush(application, "LightUpInputBrush", isLight ? "0D17212B" : "26FFFFFF");
        SetBrush(application, "LightUpInputHoverBrush", isLight ? "1417212B" : "32FFFFFF");
        SetBrush(application, "LightUpDropBrush", isLight ? "D9E7F0FF" : "D91B2A3D");
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
        SetBrush(application, "LightUpSuccessSoftBrush", isLight ? "1F0E8F5A" : "2076E3A1");
        SetBrush(application, "LightUpWarningSoftBrush", isLight ? "1FC27A00" : "20FFD166");

        ApplySemiTokens(application, accent, isLight);
    }

    private static void ApplySemiTokens(Application application, Color accent, bool isLight)
    {
        SetBrush(application, "SemiColorPrimary", accent);
        SetBrush(application, "SemiColorPrimaryPointerover", Scale(accent, isLight ? 0.84 : 1.16));
        SetBrush(application, "SemiColorPrimaryActive", Scale(accent, isLight ? 0.70 : 1.28));
        SetBrush(application, "SemiColorPrimaryDisabled", WithAlpha(accent, isLight ? (byte)0x66 : (byte)0x70));
        SetBrush(application, "SemiColorPrimaryLight", WithAlpha(accent, isLight ? (byte)0x18 : (byte)0x30));
        SetBrush(application, "SemiColorPrimaryLightPointerover", WithAlpha(accent, isLight ? (byte)0x28 : (byte)0x40));
        SetBrush(application, "SemiColorPrimaryLightActive", WithAlpha(accent, isLight ? (byte)0x38 : (byte)0x50));

        SetBrush(application, "SemiColorText0", isLight ? "FF17212B" : "FFF7FAFC");
        SetBrush(application, "SemiColorText1", isLight ? "FF52606D" : "FFC1CBD6");
        SetBrush(application, "SemiColorText2", isLight ? "FF718096" : "FF96A6B5");
        SetBrush(application, "SemiColorText3", isLight ? "FF9AA6B2" : "FF708090");
        SetBrush(application, "SemiColorBackground0", isLight ? "FFF7F9FC" : "F0141A27");
        SetBrush(application, "SemiColorBackground1", isLight ? "FFFFFFFF" : "1AFFFFFF");
        SetBrush(application, "SemiColorBackground2", isLight ? "FFF1F4F8" : "24FFFFFF");
        SetBrush(application, "SemiColorBackground3", isLight ? "FFE8EDF3" : "2EFFFFFF");
        SetBrush(application, "SemiColorBackground4", isLight ? "FFDDE5ED" : "38FFFFFF");
        SetBrush(application, "SemiColorFill0", isLight ? "0D17212B" : "1AFFFFFF");
        SetBrush(application, "SemiColorFill1", isLight ? "1417212B" : "24FFFFFF");
        SetBrush(application, "SemiColorFill2", isLight ? "1E17212B" : "32FFFFFF");
        SetBrush(application, "SemiColorBorder", isLight ? "24000000" : "32FFFFFF");
        SetBrush(application, "SemiColorFocusBorder", accent);
        SetBrush(application, "SemiColorDisabledText", isLight ? "8052606D" : "5CC1CBD6");
        SetBrush(application, "SemiColorDisabledBorder", isLight ? "18000000" : "26FFFFFF");
        SetBrush(application, "SemiColorDisabledBackground", isLight ? "0D17212B" : "0DFFFFFF");
        SetBrush(application, "SemiColorDisabledFill", isLight ? "1417212B" : "14FFFFFF");
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

    private static Color Scale(Color color, double factor)
        => Color.FromArgb(
            color.A,
            Clamp(color.R * factor),
            Clamp(color.G * factor),
            Clamp(color.B * factor));

    private static byte Clamp(double value)
        => (byte)Math.Clamp((int)Math.Round(value), 0, 255);
}
