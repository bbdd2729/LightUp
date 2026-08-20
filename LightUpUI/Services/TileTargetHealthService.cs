using System;
using System.IO;
using LightUpUI.Models.Tiles;

namespace LightUpUI.Services;

public sealed class TileTargetHealthService(
    Func<string, bool>? fileExists = null,
    Func<string, bool>? directoryExists = null) : ITileTargetHealthService
{
    private readonly Func<string, bool> _fileExists = fileExists ?? File.Exists;
    private readonly Func<string, bool> _directoryExists = directoryExists ?? Directory.Exists;

    public TileTargetHealthResult Evaluate(TileItem item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.TargetPath))
            return new TileTargetHealthResult(TileTargetHealth.Missing, "入口路径为空");

        if (item.Kind == TileItemKind.Url
            && Uri.TryCreate(item.TargetPath, UriKind.Absolute, out var uri)
            && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            && !string.IsNullOrWhiteSpace(uri.Host))
        {
            return TileTargetHealthResult.Available;
        }

        return _fileExists(item.TargetPath) || _directoryExists(item.TargetPath)
            ? TileTargetHealthResult.Available
            : new TileTargetHealthResult(TileTargetHealth.Missing, $"目标不存在：{item.TargetPath}");
    }
}
