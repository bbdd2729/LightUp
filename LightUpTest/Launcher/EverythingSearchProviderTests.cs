using LightUpUI.Models;
using LightUpUI.Services;

namespace LightUpTest.Launcher;

public sealed class EverythingSearchProviderTests
{
    [Fact]
    public async Task Search_results_are_mapped_to_file_and_folder_launcher_items()
    {
        var provider = new EverythingSearchProvider(new FakeClient(
        [
            new EverythingFileResult("C:\\Docs\\Report.pdf", false),
            new EverythingFileResult("C:\\Docs\\Projects", true),
            new EverythingFileResult("c:\\docs\\report.pdf", false)
        ]));

        var results = await provider.SearchAsync("report", TestContext.Current.CancellationToken);

        Assert.Equal(2, results.Count);
        var file = Assert.Single(results, item => item.Title == "Report.pdf");
        Assert.Equal(LauncherItemKind.File, file.Kind);
        Assert.Equal("C:\\Docs\\Report.pdf", file.LaunchPath);
        Assert.Contains("Everything", file.Subtitle);
        Assert.Equal(LauncherItemKind.Folder, Assert.Single(results, item => item.Title == "Projects").Kind);
    }

    [Theory]
    [InlineData("")]
    [InlineData("! report")]
    [InlineData("? docs")]
    [InlineData("= 2 + 2")]
    public async Task Reserved_prefixes_and_empty_queries_do_not_call_everything(string query)
    {
        var client = new FakeClient([new EverythingFileResult("C:\\report.txt", false)]);
        var provider = new EverythingSearchProvider(client);

        var results = await provider.SearchAsync(query, TestContext.Current.CancellationToken);

        Assert.Empty(results);
        Assert.Equal(0, client.CallCount);
    }

    [Fact]
    public async Task Client_cancellation_is_propagated()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var provider = new EverythingSearchProvider(new FakeClient([]));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            provider.SearchAsync("report", cancellation.Token));
    }

    private sealed class FakeClient(IReadOnlyList<EverythingFileResult> results) : IEverythingSearchClient
    {
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<EverythingFileResult>> SearchAsync(
            string query,
            int maxResults,
            CancellationToken cancellationToken)
        {
            CallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(results);
        }
    }
}
