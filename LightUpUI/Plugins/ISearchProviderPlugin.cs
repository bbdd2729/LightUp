using LightUpUI.Services;

namespace LightUpUI.Plugins;

/// <summary>
/// Contract implemented by in-process search plugins.
/// </summary>
public interface ISearchProviderPlugin
{
    string Id { get; }

    ISearchProvider CreateProvider();
}
