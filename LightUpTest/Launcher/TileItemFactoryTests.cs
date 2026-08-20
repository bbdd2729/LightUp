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
}
