using System.Globalization;
using Avalonia.Media;
using LightUpUI.Converters;
using LightUpUI.Models;
using LightUpUI.Models.Tiles;
using LightUpUI.Services;

namespace LightUpTest.Converters;

public sealed class FileIconConverterTests
{
    [Fact]
    public void Tile_converter_queries_custom_icon_before_target_path()
    {
        var paths = new List<string>();
        var converter = new TileItemNativeIconConverter(new RecordingIconService(paths));
        var item = new TileItem
        {
            TargetPath = @"C:\Apps\app.lnk",
            CustomIconPath = @"C:\Icons\custom.ico"
        };

        _ = converter.Convert(item, typeof(IImage), null, CultureInfo.InvariantCulture);

        Assert.Equal([@"C:\Icons\custom.ico", @"C:\Apps\app.lnk"], paths);
    }

    [Fact]
    public void Launcher_converter_queries_icon_path_before_launch_path()
    {
        var paths = new List<string>();
        var converter = new LauncherItemNativeIconConverter(new RecordingIconService(paths));
        var item = new LauncherItem(
            "id",
            "App",
            "Shortcut",
            @"C:\Apps\app.exe",
            null,
            LauncherItemKind.Application,
            IconPath: @"C:\Apps\app.lnk");

        _ = converter.Convert(item, typeof(IImage), null, CultureInfo.InvariantCulture);

        Assert.Equal([@"C:\Apps\app.lnk", @"C:\Apps\app.exe"], paths);
    }

    private sealed class RecordingIconService(List<string> paths) : IFileIconService
    {
        public IImage? GetIcon(string? preferredPath, string? fallbackPath, int size)
        {
            if (!string.IsNullOrWhiteSpace(preferredPath))
                paths.Add(preferredPath);
            if (!string.IsNullOrWhiteSpace(fallbackPath))
                paths.Add(fallbackPath);
            return null;
        }
    }
}
