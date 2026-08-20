using System;
using System.Collections.Generic;

namespace LightUpUI.Services;

[Flags]
public enum GlobalHotkeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8
}

public readonly record struct GlobalHotkeyGesture(GlobalHotkeyModifiers Modifiers, uint VirtualKey)
{
    public string ToConfigText()
    {
        var parts = new List<string>(5);
        if (Modifiers.HasFlag(GlobalHotkeyModifiers.Control))
            parts.Add("ctrl");
        if (Modifiers.HasFlag(GlobalHotkeyModifiers.Alt))
            parts.Add("alt");
        if (Modifiers.HasFlag(GlobalHotkeyModifiers.Shift))
            parts.Add("shift");
        if (Modifiers.HasFlag(GlobalHotkeyModifiers.Windows))
            parts.Add("win");

        parts.Add(ToKeyText(VirtualKey));
        return string.Join('+', parts);
    }

    private static string ToKeyText(uint virtualKey) => virtualKey switch
    {
        0x09 => "tab",
        0x0D => "enter",
        0x1B => "esc",
        0x20 => "space",
        >= 0x30 and <= 0x39 => ((char)virtualKey).ToString(),
        >= 0x41 and <= 0x5A => ((char)(virtualKey + 32)).ToString(),
        >= 0x70 and <= 0x87 => $"f{virtualKey - 0x6F}",
        _ => $"0x{virtualKey:X2}"
    };
}

public static class GlobalHotkeyParser
{
    public static bool TryParse(string? text, out GlobalHotkeyGesture gesture, out string? error)
    {
        gesture = default;
        error = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "快捷键不能为空。";
            return false;
        }

        var tokens = text.Split('+');
        var modifiers = GlobalHotkeyModifiers.None;
        uint? virtualKey = null;

        foreach (var rawToken in tokens)
        {
            var token = rawToken.Trim().ToLowerInvariant();
            if (token.Length == 0)
            {
                error = "快捷键中不能包含空项。";
                return false;
            }

            if (TryParseModifier(token, out var modifier))
            {
                if (modifiers.HasFlag(modifier))
                {
                    error = "快捷键修饰键不能重复。";
                    return false;
                }

                modifiers |= modifier;
                continue;
            }

            if (virtualKey is not null || !TryParseKey(token, out var parsedVirtualKey))
            {
                error = $"不支持的快捷键按键：{rawToken.Trim()}。";
                return false;
            }

            virtualKey = parsedVirtualKey;
        }

        if (modifiers == GlobalHotkeyModifiers.None)
        {
            error = "全局快捷键必须包含 Ctrl、Alt、Shift 或 Win 修饰键。";
            return false;
        }

        if (virtualKey is null)
        {
            error = "快捷键缺少主按键。";
            return false;
        }

        gesture = new GlobalHotkeyGesture(modifiers, virtualKey.Value);
        return true;
    }

    private static bool TryParseModifier(string token, out GlobalHotkeyModifiers modifier)
    {
        modifier = token switch
        {
            "ctrl" or "control" => GlobalHotkeyModifiers.Control,
            "alt" => GlobalHotkeyModifiers.Alt,
            "shift" => GlobalHotkeyModifiers.Shift,
            "win" or "windows" => GlobalHotkeyModifiers.Windows,
            _ => GlobalHotkeyModifiers.None
        };

        return modifier != GlobalHotkeyModifiers.None;
    }

    private static bool TryParseKey(string token, out uint virtualKey)
    {
        virtualKey = token switch
        {
            "tab" => 0x09,
            "enter" or "return" => 0x0D,
            "esc" or "escape" => 0x1B,
            "space" => 0x20,
            _ => 0
        };

        if (virtualKey != 0)
            return true;

        if (token.Length == 1 && char.IsAsciiLetterOrDigit(token[0]))
        {
            virtualKey = char.IsAsciiDigit(token[0])
                ? token[0]
                : char.ToUpperInvariant(token[0]);
            return true;
        }

        if (token.Length is >= 2 and <= 3 && token[0] == 'f' &&
            int.TryParse(token[1..], out var functionKey) && functionKey is >= 1 and <= 24)
        {
            virtualKey = (uint)(0x6F + functionKey);
            return true;
        }

        return false;
    }
}
