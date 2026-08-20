using Avalonia.Controls;
using Avalonia.Headless;
using LightUpUI.Models;
using LightUpUI.Models.Tiles;
using LightUpUI.Services;
using LightUpUI.ViewModels;
using LightUpUI.Views;

namespace LightUpTest.Windowing;

public sealed class TileLauncherWindowConstructionTests
{
    [Fact]
    public void Tile_launcher_window_constructs_and_resolves_its_named_title_bar()
    {
        using var session = HeadlessUnitTestSession.StartNew(
            typeof(LightUpUI.App),
            AvaloniaTestIsolationLevel.PerTest);

        session.Dispatch(
            () =>
            {
                var viewModel = new TileLauncherViewModel(new EmptyStateStore(), new SuccessfulProcessLauncher());
                var window = new TileLauncherWindow(viewModel);

                Assert.NotNull(window.FindControl<Control>("TitleBar"));
            },
            CancellationToken.None);
    }

    [Fact]
    public void Tile_launcher_sizes_the_search_box_to_half_of_its_width()
    {
        using var session = HeadlessUnitTestSession.StartNew(
            typeof(LightUpUI.App),
            AvaloniaTestIsolationLevel.PerTest);

        session.Dispatch(
            () =>
            {
                var viewModel = new TileLauncherViewModel(new EmptyStateStore(), new SuccessfulProcessLauncher());
                var window = new TileLauncherWindow(viewModel)
                {
                    Width = 1_000
                };
                var searchBox = window.FindControl<TextBox>("SearchBox");

                Assert.NotNull(searchBox);
                Assert.Equal(500, searchBox.MaxWidth);
            },
            CancellationToken.None);
    }

    private sealed class EmptyStateStore : ILauncherStateStore
    {
        public Task<TileLauncherState> LoadAsync(CancellationToken cancellationToken)
            => Task.FromResult(new TileLauncherState());

        public Task SaveAsync(TileLauncherState state, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class SuccessfulProcessLauncher : IProcessLauncher
    {
        public Task<LaunchResult> LaunchAsync(LauncherItem item, CancellationToken cancellationToken)
            => Task.FromResult(LaunchResult.Success);
    }
}
