using LightUpUI.Models.Tiles;
using LightUpUI.Services;

namespace LightUpTest.Launcher;

public sealed class TileTargetHealthTests
{
    [Fact]
    public void Evaluate_marks_existing_files_as_available()
    {
        var service = new TileTargetHealthService(
            fileExists: path => path == "C:\\Tools\\app.exe",
            directoryExists: _ => false);

        var health = service.Evaluate(new TileItem
        {
            TargetPath = "C:\\Tools\\app.exe",
            Kind = TileItemKind.Application
        });

        Assert.True(health.IsAvailable);
        Assert.Equal(TileTargetHealth.Available, health.Status);
    }

    [Fact]
    public void Evaluate_accepts_existing_directories()
    {
        var service = new TileTargetHealthService(_ => false, path => path == "C:\\Tools");

        var health = service.Evaluate(new TileItem
        {
            TargetPath = "C:\\Tools",
            Kind = TileItemKind.Folder
        });

        Assert.True(health.IsAvailable);
    }

    [Fact]
    public void Evaluate_treats_a_url_shortcut_file_as_a_local_target()
    {
        var service = new TileTargetHealthService(path => path == "C:\\Links\\Portal.url", _ => false);

        var health = service.Evaluate(new TileItem
        {
            TargetPath = "C:\\Links\\Portal.url",
            Kind = TileItemKind.Url
        });

        Assert.True(health.IsAvailable);
    }

    [Fact]
    public void Evaluate_marks_missing_local_targets_as_unavailable()
    {
        var service = new TileTargetHealthService(_ => false, _ => false);

        var health = service.Evaluate(new TileItem
        {
            TargetPath = "C:\\Missing\\app.exe",
            Kind = TileItemKind.Application
        });

        Assert.False(health.IsAvailable);
        Assert.Equal(TileTargetHealth.Missing, health.Status);
        Assert.Contains("目标不存在", health.Message);
    }

    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("ftp://example.com", false)]
    [InlineData("not a link", false)]
    public void Evaluate_validates_url_targets(string targetPath, bool expectedAvailability)
    {
        var service = new TileTargetHealthService(_ => false, _ => false);

        var health = service.Evaluate(new TileItem { TargetPath = targetPath, Kind = TileItemKind.Url });

        Assert.Equal(expectedAvailability, health.IsAvailable);
    }
}
