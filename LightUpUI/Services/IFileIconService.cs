using Avalonia.Media;

namespace LightUpUI.Services;

public interface IFileIconService
{
    IImage? GetIcon(string? preferredPath, string? fallbackPath, int size);
}
