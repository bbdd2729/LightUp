namespace LightUpUI.Models.Tiles;

public enum CategoryRemovalMode
{
    MoveItems,
    DeleteItems
}

public sealed record CategoryRemovalModeOption(CategoryRemovalMode Mode, string DisplayName);
