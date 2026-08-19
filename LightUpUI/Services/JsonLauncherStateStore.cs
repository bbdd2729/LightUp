using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models.Tiles;

namespace LightUpUI.Services;

public sealed class JsonLauncherStateStore : ILauncherStateStore
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public JsonLauncherStateStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LightUp",
            "launcher.json");
    }

    public async Task<TileLauncherState> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
            return CreateDefaultState();

        try
        {
            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<TileLauncherState>(stream, _jsonOptions, cancellationToken)
                ?? CreateDefaultState();
        }
        catch (JsonException)
        {
            return CreateDefaultState();
        }
        catch (IOException)
        {
            return CreateDefaultState();
        }
    }

    public async Task SaveAsync(TileLauncherState state, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var temporaryPath = _filePath + ".tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, state, _jsonOptions, cancellationToken);
        }

        File.Move(temporaryPath, _filePath, overwrite: true);
    }

    private static TileLauncherState CreateDefaultState() => new()
    {
        Categories =
        [
            new TileCategory { Id = "all", Name = "全部", SortOrder = 0 }
        ]
    };
}
