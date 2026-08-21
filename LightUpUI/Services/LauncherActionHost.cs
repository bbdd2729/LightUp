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
    private readonly Func<string, Task<LaunchResult>> _copyText;

    public LauncherActionHost(
        ITileLauncherWindowHost tileHost,
        Func<Task> openSettings,
        IUriLauncher? uriLauncher = null,
        IEverythingLauncher? everythingLauncher = null,
        Func<string, Task<LaunchResult>>? copyText = null)
    {
        _tileHost = tileHost;
        _openSettings = openSettings;
        _uriLauncher = uriLauncher ?? new WindowsUriLauncher();
        _everythingLauncher = everythingLauncher ?? new WindowsEverythingLauncher();
        _copyText = copyText ?? (_ => Task.FromResult(LaunchResult.Failed("当前环境不支持剪贴板")));
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
                case "action:control-panel":
                    return await _uriLauncher.OpenAsync("shell:ControlPanelFolder", cancellationToken);
                case "action:file-explorer":
                    return await _uriLauncher.OpenAsync("shell:MyComputerFolder", cancellationToken);
                case "action:copy-calculation":
                    return await _copyText(item.Arguments ?? string.Empty);
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
