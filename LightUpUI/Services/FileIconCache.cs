using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Avalonia.Media;

namespace LightUpUI.Services;

public sealed class FileIconCache(Func<string, int, IImage?> loadIcon)
{
    private readonly ConcurrentDictionary<string, Lazy<IImage?>> _icons = new(StringComparer.OrdinalIgnoreCase);

    public IImage? GetOrLoad(string? path, int size)
    {
        if (string.IsNullOrWhiteSpace(path) || size <= 0)
            return null;

        var normalizedPath = NormalizePath(path);
        var cacheKey = $"{size}|{normalizedPath}";
        var entry = _icons.GetOrAdd(
            cacheKey,
            _ => new Lazy<IImage?>(
                () => loadIcon(normalizedPath, size),
                LazyThreadSafetyMode.ExecutionAndPublication));
        return entry.Value;
    }

    private static string NormalizePath(string path)
    {
        var trimmedPath = path.Trim();
        try
        {
            return Path.GetFullPath(trimmedPath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            return trimmedPath;
        }
    }
}
