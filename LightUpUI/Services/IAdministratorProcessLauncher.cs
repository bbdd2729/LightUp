using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public interface IAdministratorProcessLauncher
{
    Task<LaunchResult> LaunchAsAdministratorAsync(
        LauncherItem item,
        CancellationToken cancellationToken);
}
