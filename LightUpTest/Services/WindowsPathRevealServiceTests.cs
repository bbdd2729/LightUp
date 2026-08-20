using LightUpUI.Services;

namespace LightUpTest.Services;

public sealed class WindowsPathRevealServiceTests
{
    [Fact]
    public async Task RevealAsync_rejects_an_empty_path_without_throwing()
    {
        var service = new WindowsPathRevealService();

        var result = await service.RevealAsync("  ", TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("路径为空", result.ErrorMessage);
    }

    [Fact]
    public async Task RevealAsync_reports_a_missing_path_without_starting_explorer()
    {
        var service = new WindowsPathRevealService();

        var result = await service.RevealAsync(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.exe"),
            TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("找不到目标", result.ErrorMessage);
    }
}
