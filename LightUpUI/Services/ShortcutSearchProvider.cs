using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public sealed class ShortcutSearchProvider : ISearchProvider
{
    private static readonly string[] SupportedExtensions = [".lnk", ".url", ".exe", ".appref-ms"];
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
        if (!OperatingSystem.IsWindows())
            return [];

        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
        };

        return roots
            .Where(Directory.Exists)
            .SelectMany(SafeEnumerateFiles)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .GroupBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(CreateItem)
            .Where(item => item is not null)
            .Cast<LauncherItem>()
            .OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> SafeEnumerateFiles(string root)
    {
        var files = new List<string>();
        try
        {
            files.AddRange(Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories));
        }
        catch
        {
        }

        return files;
    }

    private static LauncherItem? CreateItem(string path)
    {
        try
        {
            var title = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrWhiteSpace(title))
                return null;

            return new LauncherItem(
                $"shortcut:{path}",
                title,
                "开始菜单或桌面",
                path,
                null,
                Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase)
                    ? LauncherItemKind.Application
                    : LauncherItemKind.Shortcut,
                IconPath: path);
        }
        catch
        {
            return null;
        }
    }
}
