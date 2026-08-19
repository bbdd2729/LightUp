using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public sealed class WindowsProcessLauncher : IProcessLauncher
{
    public Task<LaunchResult> LaunchAsync(LauncherItem item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = item.LaunchPath,
                Arguments = item.Arguments ?? string.Empty,
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory
            });
            return Task.FromResult(LaunchResult.Success);
        }
        catch (Exception ex)
        {
            return Task.FromResult(LaunchResult.Failed($"无法启动“{item.Title}”：{ex.Message}"));
        }
    }
}
