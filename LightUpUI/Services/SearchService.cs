using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public sealed class SearchService(IEnumerable<ISearchProvider> providers) : ISearchService
{
    private readonly IReadOnlyList<ISearchProvider> _providers = providers.ToArray();

    public async Task<IReadOnlyList<LauncherItem>> SearchAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var normalizedQuery = query.Trim();
        var providerTasks = _providers.Select(provider => SearchProviderSafeAsync(provider, normalizedQuery, cancellationToken));
        var providerResults = await Task.WhenAll(providerTasks);

        return providerResults
            .SelectMany(items => items)
            .Select(item => (Item: item, Score: Score(item, normalizedQuery)))
            .Where(result => normalizedQuery.Length == 0 || result.Score > 0)
            .GroupBy(result => result.Item.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(result => result.Score)
                .ThenBy(result => result.Item.Title, StringComparer.OrdinalIgnoreCase)
                .First())
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Item.Title, StringComparer.OrdinalIgnoreCase)
            .Select(result => result.Item with { Relevance = result.Score })
            .ToArray();
    }

    private static async Task<IReadOnlyList<LauncherItem>> SearchProviderSafeAsync(
        ISearchProvider provider,
        string query,
        CancellationToken cancellationToken)
    {
        try
        {
            return await provider.SearchAsync(query, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return [];
        }
    }

    private static int Score(LauncherItem item, string query)
    {
        if (query.Length == 0)
            return item.Relevance;

        var title = item.Title.Trim();
        var subtitle = item.Subtitle.Trim();
        var path = item.LaunchPath.Trim();

        if (title.Equals(query, StringComparison.OrdinalIgnoreCase))
            return 1000 + item.Relevance;
        if (title.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            return 800 + item.Relevance;
        if (title.Contains(query, StringComparison.OrdinalIgnoreCase))
            return 600 + item.Relevance;
        if (subtitle.Contains(query, StringComparison.OrdinalIgnoreCase))
            return 300 + item.Relevance;
        if (path.Contains(query, StringComparison.OrdinalIgnoreCase))
            return 100 + item.Relevance;

        return 0;
    }
}
