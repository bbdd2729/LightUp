using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LightUpUI.Services;

public sealed class WindowsEverythingLauncher : IEverythingLauncher
{
    private readonly IEverythingExecutableLocator _executableLocator;
    private readonly Func<ProcessStartInfo, Process?> _startProcess;

    public WindowsEverythingLauncher(
        IEverythingExecutableLocator? executableLocator = null,
        Func<ProcessStartInfo, Process?>? startProcess = null)
    {
        _executableLocator = executableLocator ?? new WindowsEverythingExecutableLocator();
        _startProcess = startProcess ?? (startInfo => Process.Start(startInfo));
    }

    public Task<LaunchResult> OpenSearchAsync(string query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
            return Task.FromResult(LaunchResult.Failed("Everything 搜索目前仅支持 Windows"));

        var executablePath = _executableLocator.FindExecutablePath();
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return Task.FromResult(LaunchResult.Failed(
                "未找到 Everything.exe。请安装 Everything，或将其安装目录加入 PATH。"));
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory,
                UseShellExecute = true
            };
            if (!string.IsNullOrWhiteSpace(query))
            {
                startInfo.ArgumentList.Add("-search");
                startInfo.ArgumentList.Add(query.Trim());
            }

            return Task.FromResult(_startProcess(startInfo) is null
                ? LaunchResult.Failed("Everything 未能启动")
                : LaunchResult.Success);
        }
        catch (Exception exception)
        {
            return Task.FromResult(LaunchResult.Failed($"无法启动 Everything：{exception.Message}"));
        }
    }
}
