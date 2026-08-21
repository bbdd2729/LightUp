using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public sealed class SearchHistorySearchProvider(ISearchHistoryService historyService) : ISearchProvider
{
    public Task<IReadOnlyList<LauncherItem>> SearchAsync(
        string query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.IsNullOrWhiteSpace(query))
            return Task.FromResult<IReadOnlyList<LauncherItem>>([]);

        var results = historyService.RecentQueries
            .Select((historyQuery, index) => new LauncherItem(
                $"action:search-query:{historyQuery}",
                $"再次搜索“{historyQuery}”",
                "最近搜索 · Enter 重新搜索",
                "lightup:search-query",
                historyQuery,
                LauncherItemKind.Action,
                Relevance: historyService.RecentQueries.Count - index))
            .ToArray();

        return Task.FromResult<IReadOnlyList<LauncherItem>>(results);
    }
}
