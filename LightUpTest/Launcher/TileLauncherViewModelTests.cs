using LightUpUI.Models.Tiles;
using LightUpUI.Services;
using LightUpUI.ViewModels;

namespace LightUpTest.Launcher;

public sealed class TileLauncherViewModelTests
{
    [Fact]
    public async Task LoadAsync_selects_the_saved_category_and_exposes_its_items()
    {
        var state = new TileLauncherState
        {
            SelectedCategoryId = "dev",
            Categories =
            [
                new TileCategory { Id = "all", Name = "全部" },
                new TileCategory
                {
                    Id = "dev",
                    Name = "开发",
                    Items = [new TileItem { Id = "code", Title = "Code", TargetPath = "code.exe" }]
                }
            ]
        };
        var viewModel = new TileLauncherViewModel(new FakeStateStore(state), new FakeProcessLauncher());

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal("dev", viewModel.SelectedCategory?.Id);
        Assert.Single(viewModel.VisibleItems);
        Assert.Equal("code", viewModel.VisibleItems[0].Id);
    }

    [Fact]
    public async Task AddItem_adds_to_the_current_category_and_requests_a_save()
    {
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories = [new TileCategory { Id = "all", Name = "全部" }]
        });
        var viewModel = new TileLauncherViewModel(store, new FakeProcessLauncher());
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.AddItem(new TileItem { Id = "notes", Title = "Notes", TargetPath = "notes.exe" });

        Assert.Single(viewModel.VisibleItems);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task AddCategory_creates_selects_and_persists_a_named_category()
    {
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories = [new TileCategory { Id = "all", Name = "全部" }]
        });
        var viewModel = new TileLauncherViewModel(store, new FakeProcessLauncher());
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.AddCategory("工作");

        Assert.Equal(2, viewModel.Categories.Count);
        Assert.Equal("工作", viewModel.SelectedCategory?.Name);
        Assert.Equal(viewModel.SelectedCategory?.Id, store.State.SelectedCategoryId);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task Selecting_a_category_persists_it_as_the_next_active_category()
    {
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories =
            [
                new TileCategory { Id = "all", Name = "全部" },
                new TileCategory { Id = "work", Name = "工作" }
            ]
        });
        var viewModel = new TileLauncherViewModel(store, new FakeProcessLauncher());
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.SelectedCategory = viewModel.Categories[1];

        Assert.Equal("work", store.State.SelectedCategoryId);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task AddItemsAsync_updates_items_before_a_deferred_save_finishes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new DeferredSaveStateStore(CreateDefaultState());
        var viewModel = await CreateLoadedViewModelAsync(store, cancellationToken);

        var addTask = viewModel.AddItemsAsync(
            [CreateTile("first"), CreateTile("second")],
            cancellationToken);

        Assert.Equal(2, viewModel.VisibleItems.Count);
        Assert.True(viewModel.IsSaving);
        Assert.True(viewModel.CanEdit);

        store.CompleteSave();
        await addTask.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);

        Assert.False(viewModel.IsSaving);
        Assert.Contains("2", viewModel.StatusText);
    }

    [Fact]
    public async Task AddItemsAsync_persists_a_batch_once()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeStateStore(CreateDefaultState());
        var viewModel = await CreateLoadedViewModelAsync(store, cancellationToken);

        await viewModel.AddItemsAsync(
            [CreateTile("first"), CreateTile("second")],
            cancellationToken);

        Assert.Equal(1, store.SaveCount);
        Assert.Equal(2, viewModel.VisibleItems.Count);
    }

    [Fact]
    public async Task AddItemsAsync_reports_save_failure_without_losing_the_added_items()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FailingSaveStateStore(CreateDefaultState());
        var viewModel = await CreateLoadedViewModelAsync(store, cancellationToken);

        await viewModel.AddItemsAsync([CreateTile("broken")], cancellationToken);

        Assert.Single(viewModel.VisibleItems);
        Assert.Contains("保存失败", viewModel.StatusText);
        Assert.False(viewModel.IsSaving);
    }

    [Fact]
    public async Task AddItemsAsync_rejects_duplicate_paths_without_saving()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories =
            [
                new TileCategory
                {
                    Id = "all",
                    Name = "全部",
                    Items = [CreateTile("same")]
                }
            ]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, cancellationToken);

        await viewModel.AddItemsAsync([CreateTile("same")], cancellationToken);

        Assert.Single(viewModel.VisibleItems);
        Assert.Equal(0, store.SaveCount);
        Assert.Contains("已存在", viewModel.StatusText);
    }

    [Fact]
    public async Task Empty_state_describes_the_current_filter_and_clears_after_an_add()
    {
        var viewModel = await CreateLoadedViewModelAsync(
            new FakeStateStore(CreateDefaultState()),
            TestContext.Current.CancellationToken);

        Assert.False(viewModel.HasVisibleItems);
        Assert.Equal("当前分类还没有磁贴", viewModel.EmptyStateText);

        viewModel.SearchText = "missing";
        Assert.Equal("当前分类没有匹配的磁贴", viewModel.EmptyStateText);

        await viewModel.AddItemsAsync([CreateTile("missing")], TestContext.Current.CancellationToken);

        Assert.True(viewModel.HasVisibleItems);
        Assert.DoesNotContain("没有", viewModel.EmptyStateText);
    }

    [Fact]
    public async Task Removing_the_selected_item_clears_selection_and_updates_empty_state()
    {
        var item = CreateTile("remove-me");
        var state = CreateDefaultState();
        state.Categories[0].Items.Add(item);
        var viewModel = await CreateLoadedViewModelAsync(
            new FakeStateStore(state),
            TestContext.Current.CancellationToken);

        viewModel.SelectedItem = item;
        await viewModel.RemoveItemAsync(item, TestContext.Current.CancellationToken);

        Assert.Null(viewModel.SelectedItem);
        Assert.False(viewModel.HasVisibleItems);
        Assert.Equal("当前分类还没有磁贴", viewModel.EmptyStateText);
    }

    private static async Task<TileLauncherViewModel> CreateLoadedViewModelAsync(
        ILauncherStateStore store,
        CancellationToken cancellationToken)
    {
        var viewModel = new TileLauncherViewModel(store, new FakeProcessLauncher());
        await viewModel.LoadAsync(cancellationToken);
        return viewModel;
    }

    private static TileLauncherState CreateDefaultState() => new()
    {
        Categories = [new TileCategory { Id = "all", Name = "全部" }]
    };

    private static TileItem CreateTile(string path) => new()
    {
        Id = path,
        Title = path,
        TargetPath = path
    };

    private sealed class FakeStateStore(TileLauncherState state) : ILauncherStateStore
    {
        public int SaveCount { get; private set; }
        public TileLauncherState State { get; private set; } = state;
        public Task<TileLauncherState> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(State);
        public Task SaveAsync(TileLauncherState state, CancellationToken cancellationToken)
        {
            SaveCount++;
            State = state;
            return Task.CompletedTask;
        }
    }

    private sealed class DeferredSaveStateStore(TileLauncherState state) : ILauncherStateStore
    {
        private TaskCompletionSource? _saveCompletion;

        public int SaveCount { get; private set; }

        public Task<TileLauncherState> LoadAsync(CancellationToken cancellationToken)
            => Task.FromResult(state);

        public Task SaveAsync(TileLauncherState state, CancellationToken cancellationToken)
        {
            SaveCount++;
            _saveCompletion = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            return _saveCompletion.Task;
        }

        public void CompleteSave() => _saveCompletion?.SetResult();
    }

    private sealed class FailingSaveStateStore(TileLauncherState state) : ILauncherStateStore
    {
        public Task<TileLauncherState> LoadAsync(CancellationToken cancellationToken)
            => Task.FromResult(state);

        public Task SaveAsync(TileLauncherState state, CancellationToken cancellationToken)
            => Task.FromException(new IOException("simulated save failure"));
    }

    private sealed class FakeProcessLauncher : IProcessLauncher
    {
        public Task<LaunchResult> LaunchAsync(LightUpUI.Models.LauncherItem item, CancellationToken cancellationToken)
            => Task.FromResult(LaunchResult.Success);
    }
}
