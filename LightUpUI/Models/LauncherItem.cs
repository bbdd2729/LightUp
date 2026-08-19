namespace LightUpUI.Models;

public sealed record LauncherItem(
    string Id,
    string Title,
    string Subtitle,
    string LaunchPath,
    string? Arguments,
    LauncherItemKind Kind,
    int Relevance = 0,
    string? IconPath = null);
