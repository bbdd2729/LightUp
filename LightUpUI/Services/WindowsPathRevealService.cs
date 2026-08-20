using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LightUpUI.Services;

public sealed class WindowsPathRevealService : IPathRevealService
{
    public Task<LaunchResult> RevealAsync(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult(LaunchResult.Failed("入口路径为空"));

        try
        {
            if (Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{path}\"",
                    UseShellExecute = true
                });
                return Task.FromResult(LaunchResult.Success);
            }

            if (!File.Exists(path))
                return Task.FromResult(LaunchResult.Failed($"找不到目标：{path}"));

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{path}\"",
                UseShellExecute = true
            });
            return Task.FromResult(LaunchResult.Success);
        }
        catch (Exception exception)
        {
            return Task.FromResult(LaunchResult.Failed($"无法打开所在位置：{exception.Message}"));
        }
    }
}
