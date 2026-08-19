using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;
using LightUpUI.Models.Tiles;

namespace LightUpUI.Services;

public sealed class TileSearchProvider(ILauncherStateStore stateStore) : ISearchProvider
{
    public async Task<IReadOnlyList<LauncherItem>> SearchAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var state = await stateStore.LoadAsync(cancellationToken);
        var normalizedQuery = query.Trim();

        var items = state.Categories
            .OrderBy(category => category.SortOrder)
            .SelectMany(category => category.Items.OrderBy(item => item.SortOrder));

        if (normalizedQuery.Length == 0)
        {
            items = items
                .OrderByDescending(item => item.LastLaunchedAtUtc ?? DateTime.MinValue)
                .ThenByDescending(item => item.LaunchCount)
                .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase);
        }

        var results = items
            .Where(item => Matches(item, normalizedQuery))
            .Select(item => new LauncherItem(
                item.Id,
                item.Title,
                item.Notes ?? item.TargetPath,
                item.TargetPath,
                item.Arguments,
                MapKind(item.Kind),
                Score(item, normalizedQuery),
                item.CustomIconPath))
            .ToArray();

        return normalizedQuery.Length == 0
            ? results
            : results
                .OrderByDescending(item => item.Relevance)
                .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }

    private static bool Matches(TileItem item, string query)
    {
        if (query.Length == 0)
            return true;

        return item.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
            || item.TargetPath.Contains(query, StringComparison.OrdinalIgnoreCase)
            || item.Kind.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
            || (item.Notes?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static int Score(TileItem item, string query)
    {
        if (query.Length == 0)
            return item.LaunchCount;
        if (item.Title.Equals(query, StringComparison.OrdinalIgnoreCase))
            return 1000;
        if (item.Title.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            return 800;
        if (item.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            return 600;
        if (item.Notes?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
            return 300;
        return 100;
    }

    private static LauncherItemKind MapKind(TileItemKind kind) => kind switch
    {
        TileItemKind.Application => LauncherItemKind.Application,
        TileItemKind.Shortcut => LauncherItemKind.Shortcut,
        _ => LauncherItemKind.Shortcut
    };
}
