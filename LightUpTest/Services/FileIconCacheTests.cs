using Avalonia.Media;
using LightUpUI.Services;

namespace LightUpTest.Services;

public sealed class FileIconCacheTests
{
    [Fact]
    public void GetOrLoad_caches_a_missing_icon_for_equivalent_paths_and_size()
    {
        var calls = 0;
        var cache = new FileIconCache((_, _) =>
        {
            calls++;
            return null;
        });

        Assert.Null(cache.GetOrLoad(@"C:\Tools\App.exe", 32));
        Assert.Null(cache.GetOrLoad(@"c:\tools\APP.exe", 32));

        Assert.Equal(1, calls);
    }

    [Fact]
    public void GetOrLoad_uses_a_distinct_entry_for_each_requested_size()
    {
        var requestedSizes = new List<int>();
        var cache = new FileIconCache((_, size) =>
        {
            requestedSizes.Add(size);
            return null;
        });

        _ = cache.GetOrLoad(@"C:\Tools\App.exe", 20);
        _ = cache.GetOrLoad(@"C:\Tools\App.exe", 32);

        Assert.Equal([20, 32], requestedSizes);
    }

    [Fact]
    public void GetOrLoad_does_not_invoke_the_loader_for_an_empty_path()
    {
        var calls = 0;
        var cache = new FileIconCache((_, _) =>
        {
            calls++;
            return null;
        });

        Assert.Null(cache.GetOrLoad("  ", 32));

        Assert.Equal(0, calls);
    }
}
