using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LightUpUI.Services;

public interface ISearchHistoryService
{
    IReadOnlyList<string> RecentQueries { get; }

    Task RecordAsync(string query, CancellationToken cancellationToken);

    Task ClearAsync(CancellationToken cancellationToken);
}
