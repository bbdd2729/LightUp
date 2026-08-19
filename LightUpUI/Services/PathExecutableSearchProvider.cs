using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public sealed class PathExecutableSearchProvider : ISearchProvider
{
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private IReadOnlyList<LauncherItem>? _items;

    public async Task<IReadOnlyList<LauncherItem>> SearchAsync(
        string query,
        CancellationToken cancellationToken)
    {
        await EnsureCacheAsync(cancellationToken);
        return _items!;
    }

    private async Task EnsureCacheAsync(CancellationToken cancellationToken)
    {
        if (_items is not null)
            return;

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_items is null)
                _items = await Task.Run(BuildCache, cancellationToken);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private static IReadOnlyList<LauncherItem> BuildCache()
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var directories = pathValue
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return directories
            .SelectMany(SafeEnumerateExecutables)
            .GroupBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(path => new LauncherItem(
                $"path:{path}",
                Path.GetFileNameWithoutExtension(path),
                "PATH 程序",
                path,
                null,
                LauncherItemKind.PathExecutable,
                IconPath: path))
            .OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> SafeEnumerateExecutables(string directory)
    {
        var files = new List<string>();
        try
        {
            files.AddRange(Directory.EnumerateFiles(directory, "*.exe", SearchOption.TopDirectoryOnly));
        }
        catch
        {
        }

        return files;
    }
}
