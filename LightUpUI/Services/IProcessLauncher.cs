using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public interface IProcessLauncher
{
    Task<LaunchResult> LaunchAsync(LauncherItem item, CancellationToken cancellationToken);
}

public sealed record LaunchResult(bool Succeeded, string? ErrorMessage = null)
{
    public static LaunchResult Success { get; } = new(true);
    public static LaunchResult Failed(string message) => new(false, message);
}
