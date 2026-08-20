using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightUpUI.Models;
using LightUpUI.Models.Tiles;
using LightUpUI.Presentation;
using LightUpUI.Services;

namespace LightUpUI.ViewModels;

public partial class TileLauncherViewModel : ViewModelBase
{
    public const string DefaultCategoryId = "all";
    public const string DefaultCategoryName = "全部";

    private readonly ILauncherStateStore _stateStore;
    private readonly IProcessLauncher _processLauncher;
    private readonly IPathRevealService _pathRevealService;
    private readonly TileStateSaveCoordinator _saveCoordinator;
    private TileLauncherState _state = new();
    private bool _suppressSelectionPersistence;
    private int _pendingSaveCount;
    private RemovedTileSnapshot? _lastRemovedTile;

    private sealed record RemovedTileSnapshot(string CategoryId, TileItem Item, int SortOrder);

    public TileLauncherViewModel(
        ILauncherStateStore stateStore,
        IProcessLauncher processLauncher,
        TileStateSaveCoordinator? saveCoordinator = null,
        CategoryNavigationPlacement categoryNavigationPlacement = CategoryNavigationPlacement.Left,
        IPathRevealService? pathRevealService = null)
    {
        _stateStore = stateStore;
        _processLauncher = processLauncher;
        _pathRevealService = pathRevealService ?? new WindowsPathRevealService();
        _saveCoordinator = saveCoordinator ?? new TileStateSaveCoordinator(stateStore);
        _categoryNavigationPlacement = CategoryNavigationPlacementPolicy.Normalize(categoryNavigationPlacement);
    }

    public ObservableCollection<TileCategory> Categories { get; } = [];
    public ObservableCollection<TileItem> VisibleItems { get; } = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isOpening;

    [ObservableProperty]
    private TileCategory? _selectedCategory;

    [ObservableProperty]
    private TileItem? _selectedItem;

    [ObservableProperty]
    private TileCategory? _moveDestinationCategory;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _newCategoryName = string.Empty;

    [ObservableProperty]
    private string _editedCategoryName = string.Empty;

    [ObservableProperty]
    private string _editedTileTitle = string.Empty;

    private CategoryNavigationPlacement _categoryNavigationPlacement;

    public CategoryNavigationPlacement CategoryNavigationPlacement
    {
        get => _categoryNavigationPlacement;
        set
        {
            var normalizedPlacement = CategoryNavigationPlacementPolicy.Normalize(value);
            if (!SetProperty(ref _categoryNavigationPlacement, normalizedPlacement))
                return;

            OnPropertyChanged(nameof(IsLeftNavigation));
            OnPropertyChanged(nameof(IsTopNavigation));
        }
    }

    public bool HasVisibleItems => VisibleItems.Count > 0;

    public bool HasSelectedItem => SelectedItem is not null;

    public bool CanRenameSelectedItem => SelectedItem is not null && !IsLoading;

    public bool CanUndoLastRemoval => _lastRemovedTile is not null && !IsLoading;

    public bool ShowEmptyState => TileLauncherLayoutPolicy.ShouldShowEmptyState(IsLoading, HasVisibleItems);

    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusText);

    public bool CanEdit => !IsLoading;

    public bool CanManageSelectedCategory => SelectedCategory is not null && !IsDefaultCategory(SelectedCategory);

    public bool CanMoveSelectedItem => SelectedItem is not null
        && MoveDestinationCategory is not null
        && !ReferenceEquals(SelectedCategory, MoveDestinationCategory);

    public bool IsLeftNavigation => CategoryNavigationPlacement == CategoryNavigationPlacement.Left;

    public bool IsTopNavigation => CategoryNavigationPlacement == CategoryNavigationPlacement.Top;

    public string EmptyStateText => IsLoading
        ? "正在加载磁贴…"
        : HasVisibleItems
            ? string.Empty
            : SearchText.Trim().Length > 0
                ? "当前分类没有匹配的磁贴"
                : "当前分类还没有磁贴";

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        try
        {
            _state = await _stateStore.LoadAsync(cancellationToken) ?? new TileLauncherState();
            _state.Categories ??= [];
            foreach (var category in _state.Categories)
            {
                category.Items ??= [];
                if (string.IsNullOrWhiteSpace(category.Id))
                    category.Id = Guid.NewGuid().ToString("N");
            }

            EnsureDefaultCategory(_state.Categories);

            Categories.Clear();
            foreach (var category in _state.Categories
                         .OrderBy(category => IsDefaultCategory(category) ? 0 : 1)
                         .ThenBy(category => category.SortOrder))
                Categories.Add(category);

            SelectedCategory = Categories.FirstOrDefault(category =>
                    category.Id.Equals(_state.SelectedCategoryId, StringComparison.OrdinalIgnoreCase))
                ?? Categories[0];
            StatusText = string.Empty;
        }
        catch (OperationCanceledException)
        {
            StatusText = "加载已取消";
        }
        catch (Exception exception)
        {
            Categories.Clear();
            Categories.Add(CreateDefaultCategory());
            SelectedCategory = Categories[0];
            StatusText = $"加载失败：{exception.Message}";
        }
        finally
        {
            IsLoading = false;
            NotifyViewStateChanged();
        }
    }

    public void AddItem(TileItem item) => _ = AddItemsAsync([item]);

    public async Task AddItemsAsync(
        IEnumerable<TileItem> items,
        CancellationToken cancellationToken = default)
    {
        var category = SelectedCategory;
        if (category is null)
        {
            StatusText = "请先选择一个分类";
            return;
        }

        var candidates = (items ?? [])
            .Where(item => item is not null && !string.IsNullOrWhiteSpace(item.TargetPath))
            .ToArray();
        if (candidates.Length == 0)
        {
            StatusText = "没有可添加的入口";
            return;
        }

        var addedCount = 0;
        foreach (var item in candidates)
        {
            if (category.Items.Any(existing =>
                    existing.TargetPath.Equals(item.TargetPath, StringComparison.OrdinalIgnoreCase)))
                continue;

            if (string.IsNullOrWhiteSpace(item.Id))
                item.Id = Guid.NewGuid().ToString("N");
            if (string.IsNullOrWhiteSpace(item.Title))
                item.Title = item.TargetPath;
            item.SortOrder = category.Items.Count;
            category.Items.Add(item);
            addedCount++;
        }

        if (addedCount == 0)
        {
            StatusText = "当前分类已存在这些入口";
            return;
        }

        RefreshVisibleItems();
        if (await PersistStateAsync(cancellationToken))
            StatusText = $"已添加 {addedCount} 个入口";
    }

    public void AddCategory(string name) => _ = AddCategoryAsync(name);

    public async Task AddCategoryAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        if (normalizedName.Length == 0)
            return;

        if (Categories.Any(category => category.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = "已存在同名分类";
            return;
        }

        var category = new TileCategory
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = normalizedName,
            SortOrder = Categories.Count
        };
        Categories.Add(category);
        _suppressSelectionPersistence = true;
        try
        {
            SelectedCategory = category;
        }
        finally
        {
            _suppressSelectionPersistence = false;
        }

        if (await PersistStateAsync(cancellationToken))
        {
            NewCategoryName = string.Empty;
            StatusText = $"已创建分类“{normalizedName}”";
        }
    }

    [RelayCommand]
    public async Task RenameSelectedCategoryAsync(CancellationToken cancellationToken = default)
    {
        var category = SelectedCategory;
        if (category is null)
        {
            StatusText = "请先选择一个分类";
            return;
        }

        if (IsDefaultCategory(category))
        {
            StatusText = "“全部”分类不能重命名";
            return;
        }

        var normalizedName = EditedCategoryName.Trim();
        if (normalizedName.Length == 0)
        {
            StatusText = "分类名称不能为空";
            return;
        }

        if (Categories.Any(existing =>
                !ReferenceEquals(existing, category)
                && existing.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = "已存在同名分类";
            return;
        }

        if (category.Name.Equals(normalizedName, StringComparison.Ordinal))
        {
            StatusText = "分类名称未更改";
            return;
        }

        category.Name = normalizedName;
        NotifyCategoryChanged(category);
        if (await PersistStateAsync(cancellationToken))
            StatusText = $"已重命名为“{normalizedName}”";
    }

    [RelayCommand]
    public async Task RemoveSelectedCategoryAsync(CancellationToken cancellationToken = default)
    {
        var category = SelectedCategory;
        if (category is null)
        {
            StatusText = "请先选择一个分类";
            return;
        }

        if (IsDefaultCategory(category))
        {
            StatusText = "“全部”分类不能删除";
            return;
        }

        var defaultCategory = Categories.FirstOrDefault(IsDefaultCategory);
        if (defaultCategory is null)
        {
            defaultCategory = CreateDefaultCategory();
            Categories.Insert(0, defaultCategory);
        }

        var existingPaths = new HashSet<string>(
            defaultCategory.Items
                .Select(item => item.TargetPath)
                .Where(path => !string.IsNullOrWhiteSpace(path)),
            StringComparer.OrdinalIgnoreCase);
        var movedCount = 0;
        foreach (var item in category.Items)
        {
            if (!string.IsNullOrWhiteSpace(item.TargetPath) && !existingPaths.Add(item.TargetPath))
                continue;

            item.SortOrder = defaultCategory.Items.Count;
            defaultCategory.Items.Add(item);
            movedCount++;
        }

        var categoryName = category.Name;
        Categories.Remove(category);
        _suppressSelectionPersistence = true;
        try
        {
            SelectedCategory = defaultCategory;
        }
        finally
        {
            _suppressSelectionPersistence = false;
        }

        NotifyCategoryChanged(defaultCategory);
        if (await PersistStateAsync(cancellationToken))
        {
            StatusText = movedCount == 0
                ? $"已删除分类“{categoryName}”"
                : $"已删除分类“{categoryName}”，{movedCount} 个入口已移至“全部”";
        }
    }

    public void RemoveItem(TileItem item) => _ = RemoveItemAsync(item);

    [RelayCommand]
    public Task RemoveSelectedItemAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedItem is null)
        {
            StatusText = "请先选择一个入口";
            return Task.CompletedTask;
        }

        return RemoveItemAsync(SelectedItem, cancellationToken);
    }

    [RelayCommand]
    public async Task UndoLastRemovalAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = _lastRemovedTile;
        if (snapshot is null)
        {
            StatusText = "没有可恢复的入口";
            return;
        }

        var category = Categories.FirstOrDefault(candidate =>
            candidate.Id.Equals(snapshot.CategoryId, StringComparison.OrdinalIgnoreCase));
        if (category is null)
        {
            _lastRemovedTile = null;
            NotifyUndoStateChanged();
            StatusText = "原分类已不存在，无法恢复入口";
            return;
        }

        if (category.Items.Any(existing =>
                string.Equals(existing.TargetPath, snapshot.Item.TargetPath, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = "原分类已存在该入口，暂不恢复";
            return;
        }

        category.Items.Insert(Math.Clamp(snapshot.SortOrder, 0, category.Items.Count), snapshot.Item);
        ResetItemSortOrder(category.Items);
        if (ReferenceEquals(SelectedCategory, category))
        {
            RefreshVisibleItems();
            SelectedItem = snapshot.Item;
        }

        if (await PersistStateAsync(cancellationToken))
        {
            _lastRemovedTile = null;
            NotifyUndoStateChanged();
            StatusText = $"已恢复“{snapshot.Item.Title}”";
        }
    }

    public async Task MoveItemAsync(
        TileItem item,
        TileCategory destination,
        CancellationToken cancellationToken = default)
    {
        if (item is null || destination is null)
        {
            StatusText = "请选择要移动的入口和目标分类";
            return;
        }

        var source = Categories.FirstOrDefault(category => category.Items.Contains(item));
        if (source is null)
        {
            StatusText = "找不到入口所在的分类";
            return;
        }

        if (!Categories.Contains(destination))
        {
            StatusText = "目标分类不存在";
            return;
        }

        if (ReferenceEquals(source, destination))
        {
            StatusText = "入口已在当前分类";
            return;
        }

        if (destination.Items.Any(existing =>
                !ReferenceEquals(existing, item)
                && string.Equals(existing.TargetPath, item.TargetPath, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = "目标分类已存在该入口";
            return;
        }

        source.Items.Remove(item);
        ResetItemSortOrder(source.Items);
        destination.Items.Add(item);
        ResetItemSortOrder(destination.Items);
        RefreshVisibleItems();
        if (await PersistStateAsync(cancellationToken))
            StatusText = $"已移动“{item.Title}”至“{destination.Name}”";
    }

    [RelayCommand]
    private Task MoveSelectedItemAsync()
    {
        if (SelectedItem is null || MoveDestinationCategory is null)
        {
            StatusText = "请选择目标分类";
            return Task.CompletedTask;
        }

        return MoveItemAsync(SelectedItem, MoveDestinationCategory);
    }

    [RelayCommand]
    public async Task RenameSelectedItemAsync(CancellationToken cancellationToken = default)
    {
        var item = SelectedItem;
        if (item is null)
        {
            StatusText = "请先选择一个入口";
            return;
        }

        var normalizedTitle = EditedTileTitle.Trim();
        if (normalizedTitle.Length == 0)
        {
            StatusText = "入口名称不能为空";
            return;
        }

        if (item.Title.Equals(normalizedTitle, StringComparison.Ordinal))
        {
            StatusText = "入口名称未更改";
            return;
        }

        item.Title = normalizedTitle;
        var visibleIndex = VisibleItems.IndexOf(item);
        if (visibleIndex >= 0)
            VisibleItems[visibleIndex] = item;

        if (await PersistStateAsync(cancellationToken))
            StatusText = $"已重命名为“{normalizedTitle}”";
    }

    [RelayCommand]
    public async Task OpenContainingFolderAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedItem is null)
        {
            StatusText = "请先选择一个入口";
            return;
        }

        IsOpening = true;
        try
        {
            var item = SelectedItem;
            var result = await _pathRevealService.RevealAsync(item.TargetPath, cancellationToken);
            StatusText = result.Succeeded
                ? $"已打开所在位置：{item.Title}"
                : result.ErrorMessage ?? "无法打开所在位置";
        }
        catch (OperationCanceledException)
        {
            StatusText = "打开所在位置已取消";
        }
        catch (Exception exception)
        {
            StatusText = $"打开所在位置失败：{exception.Message}";
        }
        finally
        {
            IsOpening = false;
        }
    }

    public async Task RemoveItemAsync(
        TileItem item,
        CancellationToken cancellationToken = default)
    {
        var category = Categories.FirstOrDefault(candidate => candidate.Items.Contains(item));
        if (category is null)
            return;

        var sortOrder = category.Items.IndexOf(item);
        category.Items.RemoveAt(sortOrder);
        ResetItemSortOrder(category.Items);
        _lastRemovedTile = new RemovedTileSnapshot(category.Id, item, sortOrder);
        NotifyUndoStateChanged();
        RefreshVisibleItems();
        if (await PersistStateAsync(cancellationToken))
            StatusText = $"已移除“{item.Title}”";
    }

    public void SelectCategory(TileCategory category)
    {
        if (!Categories.Contains(category))
            return;

        SelectedCategory = category;
    }

    [RelayCommand]
    private Task AddNewCategoryAsync() => AddCategoryAsync(NewCategoryName);

    [RelayCommand]
    private async Task OpenSelectedAsync()
    {
        if (SelectedItem is null)
            return;

        IsOpening = true;
        try
        {
            var result = await _processLauncher.LaunchAsync(ToLauncherItem(SelectedItem), CancellationToken.None);
            StatusText = result.Succeeded ? $"已打开“{SelectedItem.Title}”" : result.ErrorMessage ?? "打开入口失败";
        }
        catch (Exception exception)
        {
            StatusText = $"打开失败：{exception.Message}";
        }
        finally
        {
            IsOpening = false;
        }
    }

    partial void OnSelectedCategoryChanged(TileCategory? value)
    {
        EditedCategoryName = value?.Name ?? string.Empty;
        OnPropertyChanged(nameof(CanManageSelectedCategory));
        RefreshVisibleItems();
        if (value is null)
            return;

        _state.SelectedCategoryId = value.Id;
        if (!IsLoading && !_suppressSelectionPersistence)
            _ = PersistStateAsync(CancellationToken.None);
    }

    partial void OnSelectedItemChanged(TileItem? value)
    {
        EditedTileTitle = value?.Title ?? string.Empty;
        MoveDestinationCategory = null;
        OnPropertyChanged(nameof(HasSelectedItem));
        OnPropertyChanged(nameof(CanRenameSelectedItem));
        OnPropertyChanged(nameof(CanMoveSelectedItem));
    }

    partial void OnMoveDestinationCategoryChanged(TileCategory? value)
        => OnPropertyChanged(nameof(CanMoveSelectedItem));

    partial void OnSearchTextChanged(string value)
    {
        RefreshVisibleItems();
    }

    partial void OnIsLoadingChanged(bool value)
    {
        NotifyViewStateChanged();
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(CanRenameSelectedItem));
        OnPropertyChanged(nameof(CanUndoLastRemoval));
    }

    partial void OnStatusTextChanged(string value) => OnPropertyChanged(nameof(HasStatus));

    private void RefreshVisibleItems()
    {
        VisibleItems.Clear();
        if (SelectedCategory is null)
        {
            NotifyViewStateChanged();
            return;
        }

        var query = SearchText.Trim();
        foreach (var item in SelectedCategory.Items
                     .OrderBy(item => item.SortOrder)
                     .Where(item => query.Length == 0
                         || item.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                         || item.TargetPath.Contains(query, StringComparison.OrdinalIgnoreCase)))
        {
            VisibleItems.Add(item);
        }

        if (SelectedItem is not null && !VisibleItems.Contains(SelectedItem))
            SelectedItem = null;

        NotifyViewStateChanged();
    }

    private void NotifyViewStateChanged()
    {
        OnPropertyChanged(nameof(HasVisibleItems));
        OnPropertyChanged(nameof(EmptyStateText));
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    private void NotifyUndoStateChanged()
        => OnPropertyChanged(nameof(CanUndoLastRemoval));

    private static bool IsDefaultCategory(TileCategory category)
        => category.Id.Equals(DefaultCategoryId, StringComparison.OrdinalIgnoreCase);

    private static TileCategory CreateDefaultCategory() => new()
    {
        Id = DefaultCategoryId,
        Name = DefaultCategoryName
    };

    private static void EnsureDefaultCategory(ICollection<TileCategory> categories)
    {
        var defaultCategory = categories.FirstOrDefault(IsDefaultCategory);
        if (defaultCategory is null)
        {
            categories.Add(CreateDefaultCategory());
            return;
        }

        defaultCategory.Id = DefaultCategoryId;
        defaultCategory.Name = DefaultCategoryName;
    }

    private void NotifyCategoryChanged(TileCategory category)
    {
        var index = Categories.IndexOf(category);
        if (index >= 0)
            Categories[index] = category;

        OnPropertyChanged(nameof(SelectedCategory));
    }

    private static void ResetItemSortOrder(IList<TileItem> items)
    {
        for (var index = 0; index < items.Count; index++)
            items[index].SortOrder = index;
    }

    public void ReportError(string message)
    {
        StatusText = string.IsNullOrWhiteSpace(message) ? "操作失败" : message;
    }

    private async Task<bool> PersistStateAsync(CancellationToken cancellationToken)
    {
        _state.Categories = Categories.ToList();
        _state.SelectedCategoryId = SelectedCategory?.Id ?? "all";
        _pendingSaveCount++;
        IsSaving = true;

        try
        {
            await _saveCoordinator.EnqueueAsync(_state, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            StatusText = "保存已取消";
            return false;
        }
        catch (Exception exception)
        {
            StatusText = $"保存失败：{exception.Message}";
            return false;
        }
        finally
        {
            _pendingSaveCount--;
            IsSaving = _pendingSaveCount > 0;
        }
    }

    private static LauncherItem ToLauncherItem(TileItem item) => new(
        item.Id,
        item.Title,
        item.Notes ?? item.TargetPath,
        item.TargetPath,
        item.Arguments,
        item.Kind == TileItemKind.Application ? LauncherItemKind.Application : LauncherItemKind.Shortcut,
        IconPath: item.CustomIconPath);
}
