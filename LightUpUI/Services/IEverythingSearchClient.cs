using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LightUpUI.Services;

public sealed record EverythingFileResult(string FullPath, bool IsFolder);

public interface IEverythingSearchClient
{
    Task<IReadOnlyList<EverythingFileResult>> SearchAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken);
}
