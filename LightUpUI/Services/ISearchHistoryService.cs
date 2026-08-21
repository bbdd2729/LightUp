using System.Threading;
using System.Threading.Tasks;

namespace LightUpUI.Services;

public interface ISearchHistoryService
{
    Task RecordAsync(string query, CancellationToken cancellationToken);

    Task ClearAsync(CancellationToken cancellationToken);
}
