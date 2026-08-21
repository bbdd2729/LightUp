using System.IO;

namespace LightUpTest.Windowing;

public sealed class MainWindowSearchStyleTests
{
    [Fact]
    public void Search_input_uses_the_application_text_tokens_instead_of_hardcoded_white()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "MainWindow.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("Foreground=\"{DynamicResource LightUpTextBrush}\"", markup);
        Assert.Contains("CaretBrush=\"{DynamicResource LightUpAccentBrush}\"", markup);
        Assert.DoesNotContain("Foreground=\"White\"", markup);
        Assert.DoesNotContain("CaretBrush=\"White\"", markup);
    }
}
