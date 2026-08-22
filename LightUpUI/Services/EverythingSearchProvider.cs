using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public sealed class EverythingSearchProvider : ISearchProvider
{
    private const int MaxProviderResults = 50;
    private readonly IEverythingSearchClient _client;

    public EverythingSearchProvider(IEverythingSearchClient? client = null)
        => _client = client ?? new WindowsEverythingSearchClient();

    public async Task<IReadOnlyList<LauncherItem>> SearchAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var normalizedQuery = query.Trim();
        if (normalizedQuery.Length == 0
            || normalizedQuery.StartsWith('!')
            || normalizedQuery.StartsWith('?')
            || normalizedQuery.StartsWith('='))
        {
            return [];
        }

        var files = await _client.SearchAsync(normalizedQuery, MaxProviderResults, cancellationToken);
        return files
            .Where(file => !string.IsNullOrWhiteSpace(file.FullPath))
            .GroupBy(file => file.FullPath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(CreateItem)
            .ToArray();
    }

    private static LauncherItem CreateItem(EverythingFileResult result)
    {
        var fullPath = result.FullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var title = Path.GetFileName(fullPath);
        if (string.IsNullOrWhiteSpace(title))
            title = fullPath;

        return new LauncherItem(
            $"everything:{fullPath}",
            title,
            result.IsFolder ? $"Everything · 文件夹 · {fullPath}" : $"Everything · 文件 · {fullPath}",
            fullPath,
            null,
            result.IsFolder ? LauncherItemKind.Folder : LauncherItemKind.File,
            IconPath: fullPath);
    }
}
