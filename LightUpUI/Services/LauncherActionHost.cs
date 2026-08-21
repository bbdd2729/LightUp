using System;
using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public sealed class LauncherActionHost : ILauncherActionHost
{
    private readonly ITileLauncherWindowHost _tileHost;
    private readonly Func<Task> _openSettings;
    private readonly IUriLauncher _uriLauncher;
    private readonly IEverythingLauncher _everythingLauncher;

    public LauncherActionHost(
        ITileLauncherWindowHost tileHost,
        Func<Task> openSettings,
        IUriLauncher? uriLauncher = null,
        IEverythingLauncher? everythingLauncher = null)
    {
        _tileHost = tileHost;
        _openSettings = openSettings;
        _uriLauncher = uriLauncher ?? new WindowsUriLauncher();
        _everythingLauncher = everythingLauncher ?? new WindowsEverythingLauncher();
    }

    public async Task<LaunchResult> ExecuteAsync(LauncherItem item, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (item.Id)
            {
                case "action:tiles":
                    _tileHost.Show();
                    return LaunchResult.Success;
                case "action:settings":
                    await _openSettings();
                    return LaunchResult.Success;
                case "action:everything":
                    return await _everythingLauncher.OpenSearchAsync(item.Arguments ?? string.Empty, cancellationToken);
                case "action:open-url":
                    return await _uriLauncher.OpenAsync(item.Arguments ?? string.Empty, cancellationToken);
                case "action:web-search":
                    return await _uriLauncher.OpenAsync(
                        "https://www.bing.com/search?q=" + Uri.EscapeDataString(item.Arguments ?? string.Empty),
                        cancellationToken);
                case "action:windows-settings":
                    return await _uriLauncher.OpenAsync("ms-settings:", cancellationToken);
                default:
                    return LaunchResult.Failed("未知的内建动作");
            }
        }
        catch (Exception ex)
        {
            return LaunchResult.Failed($"无法执行“{item.Title}”：{ex.Message}");
        }
    }
}
