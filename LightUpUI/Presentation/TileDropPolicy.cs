using System;
using LightUpUI.Services;

namespace LightUpUI.Presentation;

public enum TileExternalDropKind
{
    None,
    File,
    Url,
    InvalidText
}

public static class TileDropPolicy
{
    public static TileExternalDropKind Classify(bool containsFiles, string? text)
    {
        if (containsFiles)
            return TileExternalDropKind.File;

        if (TileDragPayload.TryParse(text, out _))
            return TileExternalDropKind.None;

        if (TileItemFactory.TryCreateUrl(text, out _))
            return TileExternalDropKind.Url;

        return string.IsNullOrWhiteSpace(text)
            ? TileExternalDropKind.None
            : TileExternalDropKind.InvalidText;
    }

    public static string GetFeedback(TileExternalDropKind kind) => kind switch
    {
        TileExternalDropKind.File => "释放以添加文件或文件夹",
        TileExternalDropKind.Url => "释放以添加网站快捷方式",
        TileExternalDropKind.InvalidText => "仅支持文件、文件夹或 HTTP(S) 地址",
        _ => string.Empty
    };
}
