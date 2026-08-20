using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models.Tiles;

namespace LightUpUI.Services;

public sealed class TileStateSaveCoordinator(ILauncherStateStore stateStore)
{
    private readonly ILauncherStateStore _stateStore = stateStore;
    private readonly object _gate = new();
    private Task _tail = Task.CompletedTask;

    public Task EnqueueAsync(
        TileLauncherState state,
        CancellationToken cancellationToken = default)
    {
        var snapshot = TileLauncherStateSnapshot.Clone(state);

        lock (_gate)
        {
            var previous = _tail;
            var current = SaveAfterAsync(previous, snapshot, cancellationToken);
            _tail = current;
            return current;
        }
    }

    private async Task SaveAfterAsync(
        Task previous,
        TileLauncherState snapshot,
        CancellationToken cancellationToken)
    {
        try
        {
            await previous.ConfigureAwait(false);
        }
        catch
        {
            // A failed write must not prevent the next queued snapshot from being saved.
        }

        await _stateStore.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
    }
}

internal static class TileLauncherStateSnapshot
{
    public static TileLauncherState Clone(TileLauncherState state) => new()
    {
        SelectedCategoryId = state.SelectedCategoryId,
        Categories = state.Categories
            .Select(CloneCategory)
            .ToList()
    };

    private static TileCategory CloneCategory(TileCategory category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        SortOrder = category.SortOrder,
        Items = category.Items
            .Select(CloneItem)
            .ToList()
    };

    private static TileItem CloneItem(TileItem item) => new()
    {
        Id = item.Id,
        Title = item.Title,
        TargetPath = item.TargetPath,
        Arguments = item.Arguments,
        Kind = item.Kind,
        SortOrder = item.SortOrder,
        LaunchCount = item.LaunchCount,
        LastLaunchedAtUtc = item.LastLaunchedAtUtc,
        CustomIconPath = item.CustomIconPath,
        Notes = item.Notes
    };
}
