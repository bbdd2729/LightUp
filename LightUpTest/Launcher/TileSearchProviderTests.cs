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

    private sealed class FakeStateStore(TileLauncherState state) : ILauncherStateStore
    {
        public Task<TileLauncherState> LoadAsync(CancellationToken cancellationToken)
            => Task.FromResult(state);

        public Task SaveAsync(TileLauncherState state, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
