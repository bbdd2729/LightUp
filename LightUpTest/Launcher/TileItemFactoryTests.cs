using LightUpUI.Models.Tiles;
using LightUpUI.Services;

namespace LightUpTest.Launcher;

public sealed class TileItemFactoryTests
{
    [Theory]
    [InlineData("C:\\Apps\\Code.exe", false, TileItemKind.Application, "Code")]
    [InlineData("C:\\Links\\Notes.lnk", false, TileItemKind.Shortcut, "Notes")]
    [InlineData("C:\\Links\\Portal.url", false, TileItemKind.Url, "Portal")]
    [InlineData("C:\\Docs\\Readme.txt", false, TileItemKind.File, "Readme")]
    [InlineData("C:\\Work\\Projects", true, TileItemKind.Folder, "Projects")]
    public void Create_maps_a_path_to_a_launcher_tile(
        string path,
        bool isDirectory,
        TileItemKind expectedKind,
        string expectedTitle)
    {
        var item = TileItemFactory.Create(path, _ => isDirectory);

        Assert.Equal(expectedKind, item.Kind);
        Assert.Equal(expectedTitle, item.Title);
        Assert.Equal(path, item.TargetPath);
        Assert.False(string.IsNullOrWhiteSpace(item.Id));
    }

    [Fact]
    public void Create_rejects_an_empty_target_path()
    {
        Assert.Throws<ArgumentException>(() => TileItemFactory.Create("  "));
    }

    [Theory]
    [InlineData("https://lightup.example.com/docs", "lightup.example.com")]
    [InlineData("  https://www.example.com:8443/path?q=1  ", "www.example.com")]
    public void TryCreateUrl_creates_a_url_tile_from_an_http_address(string text, string expectedTitle)
    {
        var created = TileItemFactory.TryCreateUrl(text, out var item);

        Assert.True(created);
        Assert.NotNull(item);
        Assert.Equal(TileItemKind.Url, item.Kind);
        Assert.Equal(expectedTitle, item.Title);
        Assert.Equal(text.Trim(), item.TargetPath);
        Assert.False(string.IsNullOrWhiteSpace(item.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a link")]
    [InlineData("file:///C:/Tools/LightUp.exe")]
    [InlineData("ftp://example.com/file")]
    public void TryCreateUrl_rejects_non_http_text(string text)
    {
        var created = TileItemFactory.TryCreateUrl(text, out var item);

        Assert.False(created);
        Assert.Null(item);
    }
}
