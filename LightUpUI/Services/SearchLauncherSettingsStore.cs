using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public sealed class SearchLauncherSettingsStore : ISearchLauncherSettingsStore
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public SearchLauncherSettingsStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LightUp",
            "search-launcher.json");
    }

    public async Task<SearchLauncherSettings> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
            return new SearchLauncherSettings();

        try
        {
            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<SearchLauncherSettings>(stream, _options, cancellationToken)
                ?? new SearchLauncherSettings();
        }
        catch (JsonException)
        {
            return new SearchLauncherSettings();
        }
        catch (IOException)
        {
            return new SearchLauncherSettings();
        }
    }

    public async Task SaveAsync(SearchLauncherSettings settings, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var temporaryPath = _filePath + ".tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, settings, _options, cancellationToken);
        }

        File.Move(temporaryPath, _filePath, overwrite: true);
    }
}
