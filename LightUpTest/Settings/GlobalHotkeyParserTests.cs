using LightUpUI.Services;

namespace LightUpTest.Settings;

public sealed class GlobalHotkeyParserTests
{
    [Theory]
    [InlineData("alt+space", "alt+space")]
    [InlineData("Alt + Shift + Space", "alt+shift+space")]
    [InlineData("ctrl+alt+k", "ctrl+alt+k")]
    [InlineData("win+f12", "win+f12")]
    public void TryParse_accepts_supported_shortcuts_and_returns_a_canonical_text(string input, string expected)
    {
        var parsed = GlobalHotkeyParser.TryParse(input, out var gesture, out var error);

        Assert.True(parsed, error);
        Assert.Equal(expected, gesture.ToConfigText());
    }

    [Theory]
    [InlineData("alt+alt+space")]
    [InlineData("alt+unknown")]
    [InlineData("alt")]
    [InlineData("space")]
    public void TryParse_rejects_invalid_or_unsafe_shortcuts(string input)
    {
        var parsed = GlobalHotkeyParser.TryParse(input, out _, out var error);

        Assert.False(parsed);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
