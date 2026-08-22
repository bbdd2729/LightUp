using System.IO;

namespace LightUpTest.Windowing;

public sealed class MainWindowSearchStyleTests
{
    [Fact]
    public void Search_input_uses_the_shared_application_text_tokens_instead_of_hardcoded_white()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI");
        var markup = File.ReadAllText(Path.GetFullPath(Path.Combine(root, "Views", "MainWindow.axaml")));
        var appMarkup = File.ReadAllText(Path.GetFullPath(Path.Combine(root, "App.axaml")));

        Assert.Contains("TextBox.launcher-query", appMarkup);
        Assert.Contains("Property=\"Foreground\" Value=\"{DynamicResource SemiColorText0}\"", appMarkup);
        Assert.Contains("Property=\"CaretBrush\" Value=\"{DynamicResource LightUpAccentBrush}\"", appMarkup);
        Assert.DoesNotContain("Foreground=\"White\"", markup);
        Assert.DoesNotContain("CaretBrush=\"White\"", markup);
        Assert.Contains("Classes=\"launcher-query\"", markup);
        Assert.Contains("ColumnDefinitions=\"40,*,Auto\"", markup);
        Assert.Contains("MinWidth=\"48\"", markup);
        Assert.Contains("MaxWidth=\"82\"", markup);
    }
}
