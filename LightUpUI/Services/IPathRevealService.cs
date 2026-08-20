using System.Threading;
using System.Threading.Tasks;

namespace LightUpUI.Services;

public interface IPathRevealService
{
    Task<LaunchResult> RevealAsync(string path, CancellationToken cancellationToken);
}
