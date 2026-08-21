using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public sealed class WindowsAdministratorProcessLauncher : IAdministratorProcessLauncher
{
    private readonly Func<ProcessStartInfo, Process?> _startProcess;

    public WindowsAdministratorProcessLauncher(Func<ProcessStartInfo, Process?>? startProcess = null)
        => _startProcess = startProcess ?? (startInfo => Process.Start(startInfo));

    public Task<LaunchResult> LaunchAsAdministratorAsync(
        LauncherItem item,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
            return Task.FromResult(LaunchResult.Failed("管理员启动目前仅支持 Windows"));
        if (!LauncherItemActionPolicy.CanRunAsAdministrator(item))
            return Task.FromResult(LaunchResult.Failed("此结果不支持管理员启动"));

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = item.LaunchPath,
                Arguments = item.Arguments ?? string.Empty,
                WorkingDirectory = Path.GetDirectoryName(item.LaunchPath) ?? Environment.CurrentDirectory,
                UseShellExecute = true,
                Verb = "runas"
            };
            return Task.FromResult(_startProcess(startInfo) is null
                ? LaunchResult.Failed("管理员进程未能启动")
                : LaunchResult.Success);
        }
        catch (Exception exception)
        {
            return Task.FromResult(LaunchResult.Failed($"无法以管理员身份启动“{item.Title}”：{exception.Message}"));
        }
    }
}
