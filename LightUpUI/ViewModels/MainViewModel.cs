using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightUpUI.Models;
using LightUpUI.Services;

namespace LightUpUI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ISearchService _searchService;
    private readonly IProcessLauncher _processLauncher;
    private readonly ILauncherWindowHost _windowHost;
    private CancellationTokenSource? _searchCancellation;

    public MainViewModel(
        ISearchService searchService,
        IProcessLauncher processLauncher,
        ILauncherWindowHost windowHost)
    {
        _searchService = searchService;
        _processLauncher = processLauncher;
        _windowHost = windowHost;
    }

    public MainViewModel()
        : this(new SearchService([]), new WindowsProcessLauncher(), new NullLauncherWindowHost())
    {
    }

    [ObservableProperty]
    private string _queryText = string.Empty;

    [ObservableProperty]
    private LauncherItem? _selectedItem;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _isLauncherVisible;

    [ObservableProperty]
    private SearchLauncherMode _searchMode = SearchLauncherMode.Full;

    public ObservableCollection<LauncherItem> Results { get; } = [];

    partial void OnQueryTextChanged(string value)
    {
        _ = SearchAsync(value);
    }

    partial void OnSearchModeChanged(SearchLauncherMode value)
    {
        if (!string.IsNullOrWhiteSpace(QueryText))
            _ = SearchAsync(QueryText);
    }

    public void ResetForActivation()
    {
        _searchCancellation?.Cancel();
        QueryText = string.Empty;
        Results.Clear();
        SelectedItem = null;
        StatusText = "输入应用名称开始搜索";
        IsLauncherVisible = true;
    }

    public void ResetForHide()
    {
        _searchCancellation?.Cancel();
        QueryText = string.Empty;
        Results.Clear();
        SelectedItem = null;
        IsSearching = false;
        IsLauncherVisible = false;
    }

    private async Task SearchAsync(string query)
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        var cancellationToken = _searchCancellation.Token;

        if (string.IsNullOrWhiteSpace(query))
        {
            Results.Clear();
            SelectedItem = null;
            StatusText = "输入应用名称开始搜索";
            IsSearching = false;
            return;
        }

        IsSearching = true;
        StatusText = string.Empty;
        try
        {
            var results = await _searchService.SearchAsync(SearchMode, query, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Results.Clear();
            foreach (var item in results.Take(30))
                Results.Add(item);
            SelectedItem = Results.FirstOrDefault();
            StatusText = Results.Count == 0 ? "没有找到匹配项目" : string.Empty;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Results.Clear();
            SelectedItem = null;
            StatusText = $"搜索失败：{ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void SelectPrevious()
    {
        if (Results.Count == 0)
            return;

        var index = SelectedItem is null ? 0 : Results.IndexOf(SelectedItem);
        SelectedItem = Results[index <= 0 ? Results.Count - 1 : index - 1];
    }

    [RelayCommand]
    private void SelectNext()
    {
        if (Results.Count == 0)
            return;

        var index = SelectedItem is null ? -1 : Results.IndexOf(SelectedItem);
        SelectedItem = Results[(index + 1) % Results.Count];
    }

    [RelayCommand]
    private async Task InvokeSelectedAsync()
    {
        if (SelectedItem is null)
            return;

        IsSearching = true;
        var result = await _processLauncher.LaunchAsync(SelectedItem, CancellationToken.None);
        IsSearching = false;
        if (result.Succeeded)
            _windowHost.Hide();
        else
            StatusText = result.ErrorMessage ?? "启动失败";
    }

    [RelayCommand]
    private void Hide() => _windowHost.Hide();

    private sealed class NullLauncherWindowHost : ILauncherWindowHost
    {
        public bool IsVisible => false;
        public void Toggle() { }
        public void Show() { }
        public void Hide() { }
    }
}
