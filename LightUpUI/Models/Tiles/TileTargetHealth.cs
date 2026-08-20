namespace LightUpUI.Models.Tiles;

public enum TileTargetHealth
{
    Available,
    Missing
}

public sealed record TileTargetHealthResult(TileTargetHealth Status, string? Message = null)
{
    public bool IsAvailable => Status == TileTargetHealth.Available;

    public static TileTargetHealthResult Available { get; } = new(TileTargetHealth.Available);
}
