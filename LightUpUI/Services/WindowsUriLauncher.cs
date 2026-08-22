using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LightUpUI.Services;

public sealed class WindowsUriLauncher : IUriLauncher
{
    public Task<LaunchResult> OpenAsync(string uri, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
            return Task.FromResult(LaunchResult.Failed("此操作目前仅支持 Windows"));

        if (!Uri.TryCreate(uri, UriKind.Absolute, out _))
            return Task.FromResult(LaunchResult.Failed("无效的链接"));

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });
            return Task.FromResult(LaunchResult.Success);
        }
        catch (Exception exception)
        {
            return Task.FromResult(LaunchResult.Failed($"无法打开链接：{exception.Message}"));
        }
    }
}
