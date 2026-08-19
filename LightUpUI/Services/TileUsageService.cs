using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LightUpUI.Services;

public sealed class TileUsageService(
    ILauncherStateStore stateStore,
    Func<DateTime>? utcNow = null) : ILauncherUsageTracker
{
    private readonly Func<DateTime> _utcNow = utcNow ?? (() => DateTime.UtcNow);

    public async Task RecordLaunchAsync(string itemId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return;

        var state = await stateStore.LoadAsync(cancellationToken);
        var item = state.Categories
            .SelectMany(category => category.Items)
            .FirstOrDefault(candidate => candidate.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));
        if (item is null)
            return;

        item.LaunchCount++;
        item.LastLaunchedAtUtc = _utcNow();
        await stateStore.SaveAsync(state, cancellationToken);
    }
}
