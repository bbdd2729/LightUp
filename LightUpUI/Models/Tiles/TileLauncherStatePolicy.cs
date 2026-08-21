using System;
using System.Collections.Generic;
using System.Linq;

namespace LightUpUI.Models.Tiles;

public static class TileLauncherStatePolicy
{
    public const string AllCategoryId = "all";
    public const string AllCategoryName = "全部";
    public const string UncategorizedCategoryId = "uncategorized";
    public const string UncategorizedCategoryName = "未分类";

    public static bool NormalizeForStorage(TileLauncherState state)
    {
        state.Categories ??= [];
        var changed = false;
        var allCategories = state.Categories
            .Where(IsAllCategory)
            .ToArray();
        var legacyItems = allCategories
            .SelectMany(category => category.Items ?? [])
            .ToArray();
        var uncategorized = state.Categories.FirstOrDefault(IsUncategorizedCategory);
        if (uncategorized is null && legacyItems.Length > 0)
        {
            uncategorized = new TileCategory
            {
                Id = UncategorizedCategoryId,
                Name = UncategorizedCategoryName,
                SortOrder = 0
            };
            state.Categories.Insert(0, uncategorized);
            changed = true;
        }

        foreach (var allCategory in allCategories)
        {
            var items = allCategory.Items ?? [];
            foreach (var item in items)
            {
                if (uncategorized is null)
                    break;

                if (uncategorized.Items.Any(existing =>
                        string.Equals(existing.TargetPath, item.TargetPath, StringComparison.OrdinalIgnoreCase)))
                    continue;

                uncategorized.Items.Add(item);
                changed = true;
            }

            state.Categories.Remove(allCategory);
            if (items.Count > 0)
                changed = true;
        }

        foreach (var category in state.Categories)
        {
            category.Items ??= [];
            if (IsUncategorizedCategory(category))
            {
                category.Id = UncategorizedCategoryId;
                category.Name = UncategorizedCategoryName;
            }
        }

        var orderedCategories = state.Categories
            .OrderBy(category => IsUncategorizedCategory(category) ? 0 : 1)
            .ThenBy(category => category.SortOrder)
            .ToArray();
        for (var index = 0; index < orderedCategories.Length; index++)
            orderedCategories[index].SortOrder = index;

        if (string.Equals(state.SelectedCategoryId, AllCategoryId, StringComparison.OrdinalIgnoreCase))
            return changed;

        if (!state.Categories.Any(category => category.Id.Equals(
                state.SelectedCategoryId ?? string.Empty,
                StringComparison.OrdinalIgnoreCase)))
        {
            state.SelectedCategoryId = AllCategoryId;
            changed = true;
        }

        return changed;
    }

    public static TileCategory CreateAggregateCategory(IEnumerable<TileCategory> categories)
    {
        var aggregate = new TileCategory
        {
            Id = AllCategoryId,
            Name = AllCategoryName,
            SortOrder = -1
        };
        aggregate.Items.AddRange(categories.SelectMany(category => category.Items ?? []));
        return aggregate;
    }

    public static bool IsAllCategory(TileCategory category)
        => string.Equals(category.Id, AllCategoryId, StringComparison.OrdinalIgnoreCase);

    public static bool IsUncategorizedCategory(TileCategory category)
        => string.Equals(category.Id, UncategorizedCategoryId, StringComparison.OrdinalIgnoreCase);
}
