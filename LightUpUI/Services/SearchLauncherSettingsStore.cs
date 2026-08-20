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
    private readonly Func<Stream> _openReadStream;
    private readonly SemaphoreSlim _saveGate = new(1, 1);
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public SearchLauncherSettingsStore(
        string? filePath = null,
        Func<Stream>? openReadStream = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LightUp",
            "search-launcher.json");
        _openReadStream = openReadStream ?? (() => File.OpenRead(_filePath));
    }

    public async Task<SearchLauncherSettings> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
            return new SearchLauncherSettings();

        try
        {
            using var stream = _openReadStream();
            var settings = await JsonSerializer.DeserializeAsync<SearchLauncherSettings>(
                    stream,
                    _options,
                    cancellationToken)
                .ConfigureAwait(false);
            return SearchLauncherSettingsPolicy.Normalize(settings);
        }
        catch (JsonException)
        {
            return SearchLauncherSettingsPolicy.Normalize(null);
        }
        catch (IOException)
        {
            return SearchLauncherSettingsPolicy.Normalize(null);
        }
    }

    public async Task SaveAsync(SearchLauncherSettings settings, CancellationToken cancellationToken)
    {
        await _saveGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var temporaryPath = _filePath + ".tmp";
            using (var stream = File.Create(temporaryPath))
                await JsonSerializer.SerializeAsync(
                        stream,
                        SearchLauncherSettingsPolicy.Normalize(settings),
                        _options,
                        cancellationToken)
                    .ConfigureAwait(false);

            File.Move(temporaryPath, _filePath, overwrite: true);
        }
        finally
        {
            _saveGate.Release();
        }
    }
}
