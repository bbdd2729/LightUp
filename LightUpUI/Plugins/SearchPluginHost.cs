using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LightUpUI.Models;
using LightUpUI.Services;

namespace LightUpUI.Plugins;

public sealed class SearchPluginHost
{
    public SearchPluginHost(
        IEnumerable<ISearchProviderPlugin> plugins,
        SearchLauncherSettings settings)
    {
        Providers = plugins
            .Where(plugin => IsEnabled(plugin, settings))
            .Select(plugin => CreateProviderSafely(plugin, settings))
            .Where(provider => provider is not null)
            .Cast<ISearchProvider>()
            .ToArray();
    }

    public IReadOnlyList<ISearchProvider> Providers { get; }

    public static SearchPluginHost LoadFromDirectory(
        string directoryPath,
        SearchLauncherSettings settings)
    {
        if (!Directory.Exists(directoryPath))
            return new SearchPluginHost([], settings);

        var plugins = Directory.EnumerateFiles(directoryPath, "*.dll")
            .SelectMany(LoadPluginsFromAssembly)
            .ToArray();
        return new SearchPluginHost(plugins, settings);
    }

    private static bool IsEnabled(ISearchProviderPlugin plugin, SearchLauncherSettings settings)
        => !settings.Plugins.TryGetValue(plugin.Id, out var pluginSettings)
            || pluginSettings.IsEnabled;

    private static ISearchProvider? CreateProviderSafely(
        ISearchProviderPlugin plugin,
        SearchLauncherSettings settings)
    {
        try
        {
            return new WeightedSearchProvider(
                plugin.CreateProvider(),
                GetWeight(plugin, settings));
        }
        catch
        {
            return null;
        }
    }

    private static int GetWeight(ISearchProviderPlugin plugin, SearchLauncherSettings settings)
        => settings.Plugins.TryGetValue(plugin.Id, out var pluginSettings)
            ? pluginSettings.Weight
            : 0;

    private static IEnumerable<ISearchProviderPlugin> LoadPluginsFromAssembly(string path)
    {
        Assembly assembly;
        try
        {
            assembly = Assembly.LoadFrom(path);
        }
        catch
        {
            yield break;
        }

        foreach (var type in GetLoadableTypes(assembly))
        {
            if (type.IsAbstract || !typeof(ISearchProviderPlugin).IsAssignableFrom(type))
                continue;

            ISearchProviderPlugin? plugin = null;
            try
            {
                plugin = Activator.CreateInstance(type) as ISearchProviderPlugin;
            }
            catch
            {
                // A broken plugin must not prevent the rest of the launcher from starting.
            }

            if (plugin is not null && !string.IsNullOrWhiteSpace(plugin.Id))
                yield return plugin;
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}
