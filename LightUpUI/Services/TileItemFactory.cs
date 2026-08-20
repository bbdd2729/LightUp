using System;
using System.IO;
using LightUpUI.Models.Tiles;

namespace LightUpUI.Services;

public static class TileItemFactory
{
    public static TileItem Create(
        string path,
        Func<string, bool>? directoryExists = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A tile target path is required.", nameof(path));

        var targetPath = path.Trim();
        var isDirectory = (directoryExists ?? Directory.Exists)(targetPath);
        var extension = Path.GetExtension(targetPath);
        var titleSource = targetPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var title = Path.GetFileNameWithoutExtension(titleSource);
        if (string.IsNullOrWhiteSpace(title))
            title = Path.GetFileName(titleSource);
        if (string.IsNullOrWhiteSpace(title))
            title = targetPath;

        return new TileItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = title,
            TargetPath = targetPath,
            Kind = isDirectory
                ? TileItemKind.Folder
                : extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase)
                    ? TileItemKind.Shortcut
                    : extension.Equals(".url", StringComparison.OrdinalIgnoreCase)
                        ? TileItemKind.Url
                        : extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
                            ? TileItemKind.Application
                            : TileItemKind.File
        };
    }
}
