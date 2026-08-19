using System;
using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public sealed class UsageTrackingProcessLauncher(
    IProcessLauncher inner,
    ILauncherUsageTracker usageTracker) : IProcessLauncher
{
    public async Task<LaunchResult> LaunchAsync(LauncherItem item, CancellationToken cancellationToken)
    {
        var result = await inner.LaunchAsync(item, cancellationToken);
        if (result.Succeeded)
        {
            try
            {
                await usageTracker.RecordLaunchAsync(item.Id, cancellationToken);
            }
            catch
            {
                // Usage persistence must never make a successful launch look failed.
            }
        }

        return result;
    }
}
