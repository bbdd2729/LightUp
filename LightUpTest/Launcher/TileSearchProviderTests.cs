using LightUpUI.Models.Tiles;
using LightUpUI.Services;

namespace LightUpTest.Launcher;

public sealed class TileSearchProviderTests
{
    [Fact]
    public async Task SearchAsync_returns_only_saved_tiles_matching_the_query()
    {
        var state = new TileLauncherState
        {
            Categories =
            [
                new TileCategory
                {
                    Id = "dev",
                    Name = "开发",
                    Items =
                    [
                        new TileItem { Id = "code", Title = "Visual Studio Code", TargetPath = "C:\\Tools\\Code.exe" },
                        new TileItem { Id = "notes", Title = "Notes", TargetPath = "C:\\Tools\\Notes.exe" }
                    ]
                }
            ]
        };
        var provider = new TileSearchProvider(new FakeStateStore(state));

        var results = await provider.SearchAsync("code", CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("code", results[0].Id);
        Assert.Equal("Visual Studio Code", results[0].Title);
    }

    [Fact]
    public async Task SearchAsync_does_not_scan_unstored_files()
    {
        var provider = new TileSearchProvider(new FakeStateStore(new TileLauncherState()));

        var results = await provider.SearchAsync("notepad", CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_without_a_query_orders_tiles_by_recent_use_then_launch_count()
    {
        var state = new TileLauncherState
        {
            Categories =
            [
                new TileCategory
                {
                    Id = "all",
                    Name = "全部",
                    Items =
                    [
                        new TileItem { Id = "frequent", Title = "Frequent", TargetPath = "frequent.exe", LaunchCount = 10 },
                        new TileItem { Id = "recent", Title = "Recent", TargetPath = "recent.exe", LaunchCount = 1, LastLaunchedAtUtc = new DateTime(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc) },
                        new TileItem { Id = "older", Title = "Older", TargetPath = "older.exe", LaunchCount = 50, LastLaunchedAtUtc = new DateTime(2026, 8, 19, 10, 0, 0, DateTimeKind.Utc) }
                    ]
                }
            ]
        };
        var provider = new TileSearchProvider(new FakeStateStore(state));

        var results = await provider.SearchAsync("", TestContext.Current.CancellationToken);

        Assert.Equal(["recent", "older", "frequent"], results.Select(item => item.Id));
    }

    [Fact]
    public async Task SearchAsync_uses_only_the_selected_category_when_cross_category_search_is_disabled()
    {
        var state = new TileLauncherState
        {
            SelectedCategoryId = "work",
            Categories =
            [
                new TileCategory
                {
                    Id = "work",
                    Name = "工作",
                    Items = [new TileItem { Id = "work-item", Title = "Work item", TargetPath = "work.exe" }]
                },
                new TileCategory
                {
                    Id = "play",
                    Name = "娱乐",
                    Items = [new TileItem { Id = "play-item", Title = "Play item", TargetPath = "play.exe" }]
                }
            ]
        };
        var provider = new TileSearchProvider(new FakeStateStore(state))
        {
            SearchAllTileCategories = false
        };

        var results = await provider.SearchAsync("item", CancellationToken.None);

        Assert.Equal(["work-item"], results.Select(item => item.Id));
    }

    [Fact]
    public async Task SearchAsync_falls_back_to_all_categories_when_the_saved_selection_is_missing()
    {
        var state = new TileLauncherState
        {
            SelectedCategoryId = "missing",
            Categories =
            [
                new TileCategory { Id = "work", Name = "工作", Items = [new TileItem { Id = "work-item", Title = "Work", TargetPath = "work.exe" }] },
                new TileCategory { Id = "play", Name = "娱乐", Items = [new TileItem { Id = "play-item", Title = "Play", TargetPath = "play.exe" }] }
            ]
        };
        var provider = new TileSearchProvider(new FakeStateStore(state))
        {
            SearchAllTileCategories = false
        };

        var results = await provider.SearchAsync("", CancellationToken.None);

        Assert.Equal(["play-item", "work-item"], results.Select(item => item.Id));
    }

    private sealed class FakeStateStore(TileLauncherState state) : ILauncherStateStore
    {
        public Task<TileLauncherState> LoadAsync(CancellationToken cancellationToken)
            => Task.FromResult(state);

        public Task SaveAsync(TileLauncherState state, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
