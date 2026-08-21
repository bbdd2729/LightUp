using System.Collections.Generic;
using System.IO;
using LightUpUI.Models.Tiles;
using LightUpUI.Services;

namespace LightUpTest.Launcher;

public sealed class TileStateSaveCoordinatorTests
{
    [Fact]
    public async Task EnqueueAsync_serializes_writes_and_copies_each_snapshot()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new DeferredStateStore();
        var coordinator = new TileStateSaveCoordinator(store);
        var firstState = CreateState("first");

        var firstSave = coordinator.EnqueueAsync(firstState, cancellationToken);
        firstState.Categories[0].Name = "mutated after enqueue";
        var secondSave = coordinator.EnqueueAsync(CreateState("second"), cancellationToken);

        Assert.Equal(["first"], store.StartedCategoryNames);

        store.CompleteCurrentSave();
        await firstSave.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);

        await store.WaitForStartedCountAsync(2, cancellationToken);
        Assert.Equal(["first", "second"], store.StartedCategoryNames);
        store.CompleteCurrentSave();
        await secondSave.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);

        Assert.Equal("first", store.SavedStates[0].Categories[0].Name);
        Assert.Equal("second", store.SavedStates[1].Categories[0].Name);
    }

    [Fact]
    public async Task A_failed_save_does_not_block_the_next_queued_save()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new DeferredStateStore();
        var coordinator = new TileStateSaveCoordinator(store);

        var failedSave = coordinator.EnqueueAsync(CreateState("failed"), cancellationToken);
        var laterSave = coordinator.EnqueueAsync(CreateState("later"), cancellationToken);
        store.FailCurrentSave(new IOException("simulated write failure"));

        await Assert.ThrowsAsync<IOException>(() => failedSave.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken));
        await store.WaitForStartedCountAsync(2, cancellationToken);
        Assert.Equal(["failed", "later"], store.StartedCategoryNames);

        store.CompleteCurrentSave();
        await laterSave.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
    }

    private static TileLauncherState CreateState(string categoryName) => new()
    {
        SelectedCategoryId = "category",
        Categories =
        [
            new TileCategory
            {
                Id = "category",
                Name = categoryName,
                SortOrder = 0,
                Items =
                [
                    new TileItem
                    {
                        Id = "item",
                        Title = "Item",
                        TargetPath = "item.exe",
                        SortOrder = 0
                    }
                ]
            }
        ]
    };

    private sealed class DeferredStateStore : ILauncherStateStore
    {
        private readonly object _gate = new();
        private readonly List<TaskCompletionSource> _saveCompletions = [];
        private readonly List<string> _startedCategoryNames = [];
        private readonly List<TileLauncherState> _savedStates = [];
        private TaskCompletionSource _saveStarted = CreateSignal();

        public IReadOnlyList<string> StartedCategoryNames
        {
            get
            {
                lock (_gate)
                    return _startedCategoryNames.ToArray();
            }
        }

        public IReadOnlyList<TileLauncherState> SavedStates
        {
            get
            {
                lock (_gate)
                    return _savedStates.ToArray();
            }
        }

        public Task<TileLauncherState> LoadAsync(CancellationToken cancellationToken)
            => Task.FromResult(CreateState("loaded"));

        public Task SaveAsync(TileLauncherState state, CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate)
            {
                _savedStates.Add(state);
                _startedCategoryNames.Add(state.Categories[0].Name);
                _saveCompletions.Add(completion);
                _saveStarted.TrySetResult();
                _saveStarted = CreateSignal();
            }
            return completion.Task;
        }

        public async Task WaitForStartedCountAsync(int count, CancellationToken cancellationToken)
        {
            while (true)
            {
                Task signal;
                lock (_gate)
                {
                    if (_startedCategoryNames.Count >= count)
                        return;

                    signal = _saveStarted.Task;
                }

                await signal.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        public void CompleteCurrentSave()
        {
            var completion = TakeCurrentSave();
            completion.SetResult();
        }

        public void FailCurrentSave(Exception exception)
        {
            var completion = TakeCurrentSave();
            completion.SetException(exception);
        }

        private TaskCompletionSource TakeCurrentSave()
        {
            lock (_gate)
            {
                Assert.NotEmpty(_saveCompletions);
                var completion = _saveCompletions[0];
                _saveCompletions.RemoveAt(0);
                return completion;
            }
        }

        private static TaskCompletionSource CreateSignal()
            => new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
