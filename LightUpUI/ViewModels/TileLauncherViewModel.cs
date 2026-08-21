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
    public const string DefaultCategoryId = TileLauncherStatePolicy.AllCategoryId;
    public const string DefaultCategoryName = TileLauncherStatePolicy.AllCategoryName;

    private readonly ILauncherStateStore _stateStore;
    private readonly IProcessLauncher _processLauncher;
    private readonly IPathRevealService _pathRevealService;
    private readonly ITileTargetHealthService _targetHealthService;
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
        IPathRevealService? pathRevealService = null,
        ITileTargetHealthService? targetHealthService = null,
        TileDensity tileDensity = TileDensity.Compact)
    {
        _stateStore = stateStore;
        _processLauncher = processLauncher;
        _pathRevealService = pathRevealService ?? new WindowsPathRevealService();
        _targetHealthService = targetHealthService ?? new TileTargetHealthService();
        _saveCoordinator = saveCoordinator ?? new TileStateSaveCoordinator(stateStore);
        _categoryNavigationPlacement = CategoryNavigationPlacementPolicy.Normalize(categoryNavigationPlacement);
        _tileDensity = TileDensityPolicy.Normalize(tileDensity);
    }

    public ObservableCollection<TileCategory> Categories { get; } = [];
    public IEnumerable<TileCategory> MoveDestinationCategories
        => Categories.Where(category => !IsDefaultCategory(category));
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

    [ObservableProperty]
    private string _editedTileNotes = string.Empty;

    private CategoryNavigationPlacement _categoryNavigationPlacement;

    private TileDensity _tileDensity;

    public TileDensity TileDensity
    {
        get => _tileDensity;
        set => SetProperty(ref _tileDensity, TileDensityPolicy.Normalize(value));
    }

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

    public bool CanEditSelectedItemNotes => SelectedItem is not null && !IsLoading;

    public bool CanUndoLastRemoval => _lastRemovedTile is not null && !IsLoading;

    public bool ShowEmptyState => TileLauncherLayoutPolicy.ShouldShowEmptyState(IsLoading, HasVisibleItems);

    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusText);

    public bool CanEdit => !IsLoading;

    public bool CanManageSelectedCategory => SelectedCategory is not null && !IsSystemCategory(SelectedCategory);

    public bool CanMoveSelectedCategoryUp => GetSelectedCategoryIndex() > 1;

    public bool CanMoveSelectedCategoryDown
    {
        get
        {
            if (SelectedCategory is null || IsSystemCategory(SelectedCategory))
                return false;

            var index = GetSelectedCategoryIndex();
            return index >= 1 && index < Categories.Count - 1;
        }
    }

    public bool CanMoveSelectedItem => SelectedItem is not null
        && MoveDestinationCategory is not null
        && !ReferenceEquals(SelectedCategory, MoveDestinationCategory);

    public bool CanMoveSelectedItemUp => SelectedCategory is not null
        && !IsDefaultCategory(SelectedCategory)
        && GetSelectedItemIndex() > 0;

    public bool CanMoveSelectedItemDown
    {
        get
        {
            if (SelectedCategory is null || IsDefaultCategory(SelectedCategory))
                return false;

            var index = GetSelectedItemIndex();
            return index >= 0 && index < VisibleItems.Count - 1;
        }
    }

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

                foreach (var item in category.Items)
                    UpdateTargetHealth(item);
            }

            var stateChanged = TileLauncherStatePolicy.NormalizeForStorage(_state);

            Categories.Clear();
            Categories.Add(TileLauncherStatePolicy.CreateAggregateCategory(_state.Categories));
            foreach (var category in _state.Categories
                         .OrderBy(category => category.SortOrder))
                Categories.Add(category);

            SelectedCategory = Categories.FirstOrDefault(category =>
                    category.Id.Equals(_state.SelectedCategoryId, StringComparison.OrdinalIgnoreCase))
                ?? Categories[0];
            StatusText = string.Empty;
            if (stateChanged)
                await PersistStateAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            StatusText = "加载已取消";
        }
        catch (Exception exception)
        {
            Categories.Clear();
            Categories.Add(TileLauncherStatePolicy.CreateAggregateCategory([]));
            Categories.Add(new TileCategory
            {
                Id = TileLauncherStatePolicy.UncategorizedCategoryId,
                Name = TileLauncherStatePolicy.UncategorizedCategoryName
            });
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
        var category = GetWriteCategory(SelectedCategory);
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
            UpdateTargetHealth(item);
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

        if (IsSystemCategory(category))
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

        if (IsSystemCategory(category))
        {
            StatusText = "“全部”分类不能删除";
            return;
        }

        var defaultCategory = GetUncategorizedCategory();

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
                : $"已删除分类“{categoryName}”，{movedCount} 个入口已移至“未分类”";
        }
    }

    [RelayCommand]
    public async Task MoveSelectedCategoryAsync(
        int direction,
        CancellationToken cancellationToken = default)
    {
        var category = SelectedCategory;
        if (category is null || IsSystemCategory(category))
        {
            StatusText = "“全部”分类固定在第一位";
            return;
        }

        if (direction is not (-1 or 1))
        {
            StatusText = "分类移动方向无效";
            return;
        }

        var currentIndex = Categories.IndexOf(category);
        var targetIndex = currentIndex + direction;
        if (currentIndex < 1 || targetIndex < 1 || targetIndex >= Categories.Count)
        {
            StatusText = direction < 0 ? "分类已经是第一个自定义分类" : "分类已经是最后一个分类";
            return;
        }

        Categories.Move(currentIndex, targetIndex);
        ReindexCategories();
        if (await PersistStateAsync(cancellationToken))
            StatusText = direction < 0 ? $"已上移分类“{category.Name}”" : $"已下移分类“{category.Name}”";
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

    public Task RemoveTileByIdAsync(string tileId, CancellationToken cancellationToken = default)
    {
        var category = Categories
            .Where(category => !IsDefaultCategory(category))
            .FirstOrDefault(category => category.Items.Any(item =>
                item.Id.Equals(tileId, StringComparison.OrdinalIgnoreCase)));
        var item = category?.Items.First(candidate =>
            candidate.Id.Equals(tileId, StringComparison.OrdinalIgnoreCase));
        if (item is null)
        {
            StatusText = "找不到要移除的入口";
            return Task.CompletedTask;
        }

        SelectedItem = item;
        return RemoveSelectedItemAsync(cancellationToken);
    }

    public async Task RetargetTileAsync(
        string tileId,
        TileItem replacement,
        CancellationToken cancellationToken = default)
    {
        if (replacement is null || string.IsNullOrWhiteSpace(replacement.TargetPath))
        {
            StatusText = "目标路径不能为空";
            return;
        }

        var category = Categories
            .Where(candidate => !IsDefaultCategory(candidate))
            .FirstOrDefault(candidate =>
            candidate.Items.Any(item => item.Id.Equals(tileId, StringComparison.OrdinalIgnoreCase)));
        if (category is null)
        {
            StatusText = "找不到要重新定位的入口";
            return;
        }

        var item = category.Items.First(candidate =>
            candidate.Id.Equals(tileId, StringComparison.OrdinalIgnoreCase));
        var targetPath = replacement.TargetPath.Trim();
        if (category.Items.Any(candidate =>
                !ReferenceEquals(candidate, item)
                && candidate.TargetPath.Equals(targetPath, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = "当前分类已存在该目标";
            return;
        }

        item.TargetPath = targetPath;
        item.Arguments = replacement.Arguments;
        item.Kind = replacement.Kind;
        item.CustomIconPath = replacement.CustomIconPath;
        UpdateTargetHealth(item);
        if (ReferenceEquals(SelectedCategory, category))
            RefreshVisibleItems();

        SelectedItem = item;
        if (await PersistStateAsync(cancellationToken))
            StatusText = $"已重新定位“{item.Title}”";
    }

    public async Task SetTileCustomIconAsync(
        string tileId,
        string? customIconPath,
        CancellationToken cancellationToken = default)
    {
        var category = Categories
            .Where(candidate => !IsDefaultCategory(candidate))
            .FirstOrDefault(candidate =>
                candidate.Items.Any(item => item.Id.Equals(tileId, StringComparison.OrdinalIgnoreCase)));
        if (category is null)
        {
            StatusText = "找不到要设置图标的入口";
            return;
        }

        var item = category.Items.First(candidate =>
            candidate.Id.Equals(tileId, StringComparison.OrdinalIgnoreCase));
        var normalizedPath = string.IsNullOrWhiteSpace(customIconPath) ? null : customIconPath.Trim();
        if (string.Equals(item.CustomIconPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
        {
            StatusText = normalizedPath is null ? "当前正在使用默认图标" : "自定义图标未变化";
            return;
        }

        item.CustomIconPath = normalizedPath;
        if (SelectedCategory is not null)
            RefreshVisibleItems();

        SelectedItem = item;
        if (await PersistStateAsync(cancellationToken))
        {
            StatusText = normalizedPath is null
                ? $"已恢复“{item.Title}”的默认图标"
                : $"已设置“{item.Title}”的自定义图标";
        }
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

        var source = FindOwningCategory(item);
        if (source is null)
        {
            StatusText = "找不到入口所在的分类";
            return;
        }

        if (!Categories.Contains(destination) || IsDefaultCategory(destination))
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

    public Task MoveTileByIdToCategoryAsync(
        string tileId,
        TileCategory destination,
        CancellationToken cancellationToken = default)
    {
        var item = Categories
            .Where(category => !IsDefaultCategory(category))
            .SelectMany(category => category.Items)
            .FirstOrDefault(candidate => candidate.Id.Equals(tileId, StringComparison.OrdinalIgnoreCase));
        if (item is null)
        {
            StatusText = "找不到要移动的入口";
            return Task.CompletedTask;
        }

        SelectedItem = item;
        return MoveItemAsync(item, destination, cancellationToken);
    }

    public async Task MoveTileByIdWithinCategoryAsync(
        string tileId,
        string targetTileId,
        bool insertAfterTarget,
        CancellationToken cancellationToken = default)
    {
        if (SelectedCategory is not null && IsDefaultCategory(SelectedCategory))
        {
            StatusText = "“全部”只用于汇总显示，不能调整入口顺序";
            return;
        }

        var category = Categories
            .Where(candidate => !IsDefaultCategory(candidate))
            .FirstOrDefault(candidate =>
            candidate.Items.Any(item => item.Id.Equals(tileId, StringComparison.OrdinalIgnoreCase)));
        if (category is null)
        {
            StatusText = "找不到要排序的入口";
            return;
        }

        var item = category.Items.First(candidate =>
            candidate.Id.Equals(tileId, StringComparison.OrdinalIgnoreCase));
        var target = category.Items.FirstOrDefault(candidate =>
            candidate.Id.Equals(targetTileId, StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            StatusText = "排序目标不存在或不在当前分类";
            return;
        }

        if (ReferenceEquals(item, target))
        {
            StatusText = "入口无需调整顺序";
            return;
        }

        category.Items.Remove(item);
        var targetIndex = category.Items.IndexOf(target);
        category.Items.Insert(targetIndex + (insertAfterTarget ? 1 : 0), item);
        ResetItemSortOrder(category.Items);
        if (ReferenceEquals(SelectedCategory, category))
            RefreshVisibleItems();

        SelectedItem = item;
        NotifyItemOrderChanged();
        if (await PersistStateAsync(cancellationToken))
            StatusText = $"已调整顺序：“{item.Title}”";
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
    private Task MoveSelectedItemUpAsync()
        => MoveSelectedItemWithinCategoryAsync(-1);

    [RelayCommand]
    private Task MoveSelectedItemDownAsync()
        => MoveSelectedItemWithinCategoryAsync(1);

    [RelayCommand]
    public async Task MoveSelectedItemWithinCategoryAsync(
        int direction,
        CancellationToken cancellationToken = default)
    {
        var item = SelectedItem;
        var category = SelectedCategory;
        if (item is null || category is null)
        {
            StatusText = "请先选择一个入口";
            return;
        }

        if (IsDefaultCategory(category))
        {
            StatusText = "“全部”只用于汇总显示，不能调整入口顺序";
            return;
        }

        if (direction is not (-1 or 1))
        {
            StatusText = "入口移动方向无效";
            return;
        }

        var currentIndex = category.Items.IndexOf(item);
        var targetIndex = currentIndex + direction;
        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= category.Items.Count)
        {
            StatusText = direction < 0 ? "入口已经是第一个" : "入口已经是最后一个";
            return;
        }

        category.Items.RemoveAt(currentIndex);
        category.Items.Insert(targetIndex, item);
        ResetItemSortOrder(category.Items);
        RefreshVisibleItems();
        SelectedItem = item;
        NotifyItemOrderChanged();
        if (await PersistStateAsync(cancellationToken))
            StatusText = direction < 0 ? $"已上移“{item.Title}”" : $"已下移“{item.Title}”";
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

    [RelayCommand]
    public async Task SaveSelectedItemNotesAsync(CancellationToken cancellationToken = default)
    {
        var item = SelectedItem;
        if (item is null)
        {
            StatusText = "请先选择一个入口";
            return;
        }

        var normalizedNotes = EditedTileNotes.Trim();
        var nextNotes = normalizedNotes.Length == 0 ? null : normalizedNotes;
        if (string.Equals(item.Notes, nextNotes, StringComparison.Ordinal))
        {
            StatusText = "备注未更改";
            return;
        }

        item.Notes = nextNotes;
        EditedTileNotes = nextNotes ?? string.Empty;
        var visibleIndex = VisibleItems.IndexOf(item);
        if (visibleIndex >= 0)
            VisibleItems[visibleIndex] = item;

        if (await PersistStateAsync(cancellationToken))
            StatusText = nextNotes is null ? "已清除备注" : "已保存备注";
    }

    public async Task RemoveItemAsync(
        TileItem item,
        CancellationToken cancellationToken = default)
    {
        var category = FindOwningCategory(item);
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
    private Task MoveSelectedCategoryUpAsync()
        => MoveSelectedCategoryAsync(-1);

    [RelayCommand]
    private Task MoveSelectedCategoryDownAsync()
        => MoveSelectedCategoryAsync(1);

    [RelayCommand]
    private async Task OpenSelectedAsync()
    {
        if (SelectedItem is null)
            return;

        UpdateTargetHealth(SelectedItem);
        if (!SelectedItem.IsTargetAvailable)
        {
            StatusText = SelectedItem.TargetHealthMessage ?? "目标不可用";
            RefreshVisibleItems();
            return;
        }

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
        NotifyCategoryOrderChanged();
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
        EditedTileNotes = value?.Notes ?? string.Empty;
        MoveDestinationCategory = null;
        OnPropertyChanged(nameof(HasSelectedItem));
        OnPropertyChanged(nameof(CanRenameSelectedItem));
        OnPropertyChanged(nameof(CanEditSelectedItemNotes));
        OnPropertyChanged(nameof(CanMoveSelectedItem));
        NotifyItemOrderChanged();
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
        OnPropertyChanged(nameof(CanEditSelectedItemNotes));
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
        var items = IsDefaultCategory(SelectedCategory)
            ? TileLauncherStatePolicy.CreateAggregateCategory(
                Categories.Where(category => !IsDefaultCategory(category))).Items
            : SelectedCategory.Items;
        foreach (var item in items
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

    private void NotifyCategoryOrderChanged()
    {
        OnPropertyChanged(nameof(CanMoveSelectedCategoryUp));
        OnPropertyChanged(nameof(CanMoveSelectedCategoryDown));
    }

    private void NotifyItemOrderChanged()
    {
        OnPropertyChanged(nameof(CanMoveSelectedItemUp));
        OnPropertyChanged(nameof(CanMoveSelectedItemDown));
    }

    private static bool IsDefaultCategory(TileCategory category)
        => TileLauncherStatePolicy.IsAllCategory(category);

    private static bool IsSystemCategory(TileCategory category)
        => TileLauncherStatePolicy.IsAllCategory(category)
            || TileLauncherStatePolicy.IsUncategorizedCategory(category);

    private TileCategory GetUncategorizedCategory()
    {
        var category = Categories.FirstOrDefault(TileLauncherStatePolicy.IsUncategorizedCategory);
        if (category is not null)
            return category;

        category = new TileCategory
        {
            Id = TileLauncherStatePolicy.UncategorizedCategoryId,
            Name = TileLauncherStatePolicy.UncategorizedCategoryName,
            SortOrder = Categories.Count
        };
        Categories.Add(category);
        return category;
    }

    private TileCategory GetWriteCategory(TileCategory? category)
        => category is not null && !IsDefaultCategory(category) ? category : GetUncategorizedCategory();

    private TileCategory? FindOwningCategory(TileItem item)
        => Categories.FirstOrDefault(category => !IsDefaultCategory(category) && category.Items.Contains(item));

    private void NotifyCategoryChanged(TileCategory category)
    {
        var index = Categories.IndexOf(category);
        if (index >= 0)
            Categories[index] = category;

        OnPropertyChanged(nameof(SelectedCategory));
    }

    private int GetSelectedCategoryIndex()
        => SelectedCategory is null ? -1 : Categories.IndexOf(SelectedCategory);

    private void ReindexCategories()
    {
        for (var index = 0; index < Categories.Count; index++)
            Categories[index].SortOrder = index;

        NotifyCategoryOrderChanged();
    }

    private static void ResetItemSortOrder(IList<TileItem> items)
    {
        for (var index = 0; index < items.Count; index++)
            items[index].SortOrder = index;
    }

    private int GetSelectedItemIndex()
        => SelectedCategory is null || SelectedItem is null
            ? -1
            : SelectedCategory.Items.IndexOf(SelectedItem);

    public void ReportError(string message)
    {
        StatusText = string.IsNullOrWhiteSpace(message) ? "操作失败" : message;
    }

    private async Task<bool> PersistStateAsync(CancellationToken cancellationToken)
    {
        _state.Categories = Categories
            .Where(category => !IsDefaultCategory(category))
            .ToList();
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

    private void UpdateTargetHealth(TileItem item)
    {
        var health = _targetHealthService.Evaluate(item);
        item.TargetHealth = health.Status;
        item.TargetHealthMessage = health.Message;
    }
}
