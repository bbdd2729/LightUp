using System.Threading;
using System.Threading.Tasks;

namespace LightUpUI.Services;

public interface IUriLauncher
{
    Task<LaunchResult> OpenAsync(string uri, CancellationToken cancellationToken);
}
