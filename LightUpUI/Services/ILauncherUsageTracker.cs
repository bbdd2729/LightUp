using System.Threading;
using System.Threading.Tasks;

namespace LightUpUI.Services;

public interface ILauncherUsageTracker
{
    Task RecordLaunchAsync(string itemId, CancellationToken cancellationToken);
}
