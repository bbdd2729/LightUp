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

    private sealed class FakeProcessLauncher : IProcessLauncher
    {
        public Task<LaunchResult> LaunchAsync(LightUpUI.Models.LauncherItem item, CancellationToken cancellationToken)
            => Task.FromResult(LaunchResult.Success);
    }
}
