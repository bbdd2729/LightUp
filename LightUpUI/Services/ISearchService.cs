using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public interface ISearchService
{
    Task<IReadOnlyList<LauncherItem>> SearchAsync(
        string query,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<LauncherItem>> SearchAsync(
        SearchLauncherMode mode,
        string query,
        CancellationToken cancellationToken);
}
