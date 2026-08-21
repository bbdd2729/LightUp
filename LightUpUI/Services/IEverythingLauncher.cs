using System.Threading;
using System.Threading.Tasks;

namespace LightUpUI.Services;

public interface IEverythingLauncher
{
    Task<LaunchResult> OpenSearchAsync(string query, CancellationToken cancellationToken);
}
