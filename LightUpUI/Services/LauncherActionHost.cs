using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using LightUpUI.Models;

namespace LightUpUI.Services;

public sealed class LauncherActionHost(
    ITileLauncherWindowHost tileHost,
    Func<Task> openSettings) : ILauncherActionHost
{
    public async Task<LaunchResult> ExecuteAsync(LauncherItem item, CancellationToken cancellationToken)
    {
        try
        {
            switch (item.Id)
            {
                case "action:tiles":
                    tileHost.Show();
                    return LaunchResult.Success;
                case "action:settings":
                    await openSettings();
                    return LaunchResult.Success;
                case "action:everything":
                    if (!OperatingSystem.IsWindows())
                        return LaunchResult.Failed("Everything 搜索目前仅支持 Windows");

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "es:"
                            + Uri.EscapeDataString(item.Arguments ?? string.Empty),
                        UseShellExecute = true
                    });
                    return LaunchResult.Success;
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
