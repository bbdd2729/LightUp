using System;

namespace LightUpUI.Presentation;

public static class TileDragPayload
{
    private const string Prefix = "lightup-tile:";

    public static string Create(string tileId)
        => string.IsNullOrWhiteSpace(tileId) ? string.Empty : Prefix + tileId.Trim();

    public static bool TryParse(string? payload, out string tileId)
    {
        tileId = string.Empty;
        if (string.IsNullOrWhiteSpace(payload)
            || !payload.StartsWith(Prefix, StringComparison.Ordinal)
            || payload.Length <= Prefix.Length)
            return false;

        tileId = payload[Prefix.Length..].Trim();
        return tileId.Length > 0;
    }
}
