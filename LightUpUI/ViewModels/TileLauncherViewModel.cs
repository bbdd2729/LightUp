using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightUpUI.Models;
using LightUpUI.Models.Tiles;
using LightUpUI.Services;

namespace LightUpUI.ViewModels;

public partial class TileLauncherViewModel : ViewModelBase
{
    private readonly ILauncherStateStore _stateStore;
    private readonly IProcessLauncher _processLauncher;
    private TileLauncherState _state = new();
    private bool _isLoading;

    public TileLauncherViewModel(ILauncherStateStore stateStore, IProcessLauncher processLauncher)
    {
        _stateStore = stateStore;
        _processLauncher = processLauncher;
    }

    public ObservableCollection<TileCategory> Categories { get; } = [];
    public ObservableCollection<TileItem> VisibleItems { get; } = [];

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

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _isLoading = true;
        try
        {
            _state = await _stateStore.LoadAsync(cancellationToken);
            if (_state.Categories.Count == 0)
            {
                _state.Categories.Add(new TileCategory { Id = "all", Name = "全部" });
            }

            Categories.Clear();
            foreach (var category in _state.Categories.OrderBy(category => category.SortOrder))
                Categories.Add(category);

            SelectedCategory = Categories.FirstOrDefault(category => category.Id == _state.SelectedCategoryId)
                ?? Categories[0];
            StatusText = string.Empty;
        }
        finally
        {
            _isLoading = false;
        }
    }

    public void AddItem(TileItem item)
    {
        if (SelectedCategory is null || string.IsNullOrWhiteSpace(item.TargetPath))
            return;

        if (SelectedCategory.Items.Any(existing =>
                existing.TargetPath.Equals(item.TargetPath, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = "当前分类已存在此入口";
            return;
        }

        item.SortOrder = SelectedCategory.Items.Count;
        SelectedCategory.Items.Add(item);
        RefreshVisibleItems();
        SaveState();
    }

    public void AddCategory(string name)
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
        SelectedCategory = category;
        NewCategoryName = string.Empty;
    }

    public void RemoveItem(TileItem item)
    {
        if (SelectedCategory?.Items.Remove(item) != true)
            return;

        RefreshVisibleItems();
        SaveState();
    }

    public void SelectCategory(TileCategory category)
    {
        if (!Categories.Contains(category))
            return;

        SelectedCategory = category;
    }

    [RelayCommand]
    private void AddNewCategory() => AddCategory(NewCategoryName);

    [RelayCommand]
    private async Task OpenSelectedAsync()
    {
        if (SelectedItem is null)
            return;

        var result = await _processLauncher.LaunchAsync(ToLauncherItem(SelectedItem), CancellationToken.None);
        StatusText = result.Succeeded ? string.Empty : result.ErrorMessage ?? "打开入口失败";
    }

    partial void OnSelectedCategoryChanged(TileCategory? value)
    {
        RefreshVisibleItems();
        if (value is null)
            return;

        _state.SelectedCategoryId = value.Id;
        if (!_isLoading)
            SaveState();
    }

    partial void OnSearchTextChanged(string value)
    {
        RefreshVisibleItems();
    }

    private void RefreshVisibleItems()
    {
        VisibleItems.Clear();
        if (SelectedCategory is null)
            return;

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
    }

    private void SaveState()
    {
        _state.Categories = Categories.ToList();
        _state.SelectedCategoryId = SelectedCategory?.Id ?? "all";
        _stateStore.SaveAsync(_state, CancellationToken.None).GetAwaiter().GetResult();
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
