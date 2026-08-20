using LightUpUI.Services;

namespace LightUpTest.Services;

public sealed class WindowsFileIconServiceTests
{
    [Fact]
    public void GetIcon_tries_the_preferred_path_before_the_target_fallback()
    {
        var paths = new List<string>();
        var service = new WindowsFileIconService((path, _) =>
        {
            paths.Add(path);
            return null;
        });

        Assert.Null(service.GetIcon(@"C:\Icons\custom.ico", @"C:\Apps\app.lnk", 48));

        Assert.Equal([@"C:\Icons\custom.ico", @"C:\Apps\app.lnk"], paths);
    }

    [Fact]
    public void GetIcon_does_not_load_the_same_preferred_and_fallback_path_twice()
    {
        var calls = 0;
        var service = new WindowsFileIconService((_, _) =>
        {
            calls++;
            return null;
        });

        Assert.Null(service.GetIcon(@"C:\Apps\app.lnk", @"C:\Apps\app.lnk", 48));

        Assert.Equal(1, calls);
    }
}
