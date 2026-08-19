using System;
using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public sealed class LauncherProcessRouter(
    IProcessLauncher processLauncher,
    Func<LauncherItem, CancellationToken, Task<LaunchResult>> actionHandler) : IProcessLauncher
{
    public Task<LaunchResult> LaunchAsync(LauncherItem item, CancellationToken cancellationToken)
        => item.Kind == LauncherItemKind.Action
            ? actionHandler(item, cancellationToken)
            : processLauncher.LaunchAsync(item, cancellationToken);
}
