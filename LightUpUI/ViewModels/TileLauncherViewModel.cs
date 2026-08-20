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
    private readonly ILauncherStateStore _stateStore;
    private readonly IProcessLauncher _processLauncher;
    private readonly TileStateSaveCoordinator _saveCoordinator;
    private TileLauncherState _state = new();
    private bool _suppressSelectionPersistence;
    private int _pendingSaveCount;

    public TileLauncherViewModel(
        ILauncherStateStore stateStore,
        IProcessLauncher processLauncher,
        TileStateSaveCoordinator? saveCoordinator = null)
    {
        _stateStore = stateStore;
        _processLauncher = processLauncher;
        _saveCoordinator = saveCoordinator ?? new TileStateSaveCoordinator(stateStore);
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
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _newCategoryName = string.Empty;

    public bool HasVisibleItems => VisibleItems.Count > 0;

    public bool ShowEmptyState => TileLauncherLayoutPolicy.ShouldShowEmptyState(IsLoading, HasVisibleItems);

    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusText);

    public bool CanEdit => !IsLoading;

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
            if (_state.Categories.Count == 0)
            {
                _state.Categories.Add(new TileCategory { Id = "all", Name = "全部" });
            }

            foreach (var category in _state.Categories)
            {
                category.Items ??= [];
                if (string.IsNullOrWhiteSpace(category.Id))
                    category.Id = Guid.NewGuid().ToString("N");
            }

            Categories.Clear();
            foreach (var category in _state.Categories.OrderBy(category => category.SortOrder))
                Categories.Add(category);

            SelectedCategory = Categories.FirstOrDefault(category => category.Id == _state.SelectedCategoryId)
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
            Categories.Add(new TileCategory { Id = "all", Name = "全部" });
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

    public void RemoveItem(TileItem item) => _ = RemoveItemAsync(item);

    public async Task RemoveItemAsync(
        TileItem item,
        CancellationToken cancellationToken = default)
    {
        if (SelectedCategory?.Items.Remove(item) != true)
            return;

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
        RefreshVisibleItems();
        if (value is null)
            return;

        _state.SelectedCategoryId = value.Id;
        if (!IsLoading && !_suppressSelectionPersistence)
            _ = PersistStateAsync(CancellationToken.None);
    }

    partial void OnSearchTextChanged(string value)
    {
        RefreshVisibleItems();
    }

    partial void OnIsLoadingChanged(bool value)
    {
        NotifyViewStateChanged();
        OnPropertyChanged(nameof(CanEdit));
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
