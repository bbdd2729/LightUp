using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using LightUpUI.Models.Tiles;

namespace LightUpUI.Services;

public static class TileItemFactory
{
    public static bool TryCreateUrl(string? value, [NotNullWhen(true)] out TileItem? item)
    {
        item = null;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var targetPath = value.Trim();
        if (!Uri.TryCreate(targetPath, UriKind.Absolute, out var uri)
            || (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        item = new TileItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = uri.Host,
            TargetPath = targetPath,
            Kind = TileItemKind.Url
        };
        return true;
    }

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
