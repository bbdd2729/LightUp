using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public interface ILauncherActionHost
{
    Task<LaunchResult> ExecuteAsync(LauncherItem item, CancellationToken cancellationToken);
}
