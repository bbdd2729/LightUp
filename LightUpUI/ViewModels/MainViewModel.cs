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
    private readonly IPathRevealService _pathRevealService;
    private readonly ISearchHistoryService? _searchHistoryService;
    private CancellationTokenSource? _searchCancellation;

    public MainViewModel(
        ISearchService searchService,
        IProcessLauncher processLauncher,
        ILauncherWindowHost windowHost,
        IPathRevealService? pathRevealService = null,
        ISearchHistoryService? searchHistoryService = null)
    {
        _searchService = searchService;
        _processLauncher = processLauncher;
        _windowHost = windowHost;
        _pathRevealService = pathRevealService ?? new WindowsPathRevealService();
        _searchHistoryService = searchHistoryService;
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

    private int _maxResults = SearchResultLimitPolicy.DefaultLimit;

    public string SearchModeLabel => SearchMode == SearchLauncherMode.Simple ? "简约模式" : "完整模式";

    public int MaxResults
    {
        get => _maxResults;
        set => SetProperty(ref _maxResults, SearchResultLimitPolicy.Normalize(value));
    }

    public ObservableCollection<LauncherItem> Results { get; } = [];

    partial void OnQueryTextChanged(string value)
    {
        _ = SearchAsync(value);
    }

    partial void OnSearchModeChanged(SearchLauncherMode value)
    {
        OnPropertyChanged(nameof(SearchModeLabel));
        if (!string.IsNullOrWhiteSpace(QueryText))
            _ = SearchAsync(QueryText);
    }

    [RelayCommand]
    private void ClearQuery() => QueryText = string.Empty;

    public void ResetForActivation()
    {
        _searchCancellation?.Cancel();
        QueryText = string.Empty;
        Results.Clear();
        SelectedItem = null;
        StatusText = "输入应用名称开始搜索";
        IsLauncherVisible = true;
        _ = SearchAsync(string.Empty);
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

        IsSearching = true;
        StatusText = string.Empty;
        try
        {
            var results = await _searchService.SearchAsync(SearchMode, query, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Results.Clear();
            var maxResults = SearchResultLimitPolicy.GetVisibleResultLimit(
                MaxResults,
                string.IsNullOrWhiteSpace(query));
            foreach (var item in results.Take(maxResults))
                Results.Add(item);
            SelectedItem = Results.FirstOrDefault();
            StatusText = Results.Count == 0
                ? string.IsNullOrWhiteSpace(query) ? "暂无可显示的最近项目" : "没有找到匹配项目"
                : string.Empty;
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
        var item = SelectedItem;
        if (item is null)
            return;

        IsSearching = true;
        if (LauncherItemActionPolicy.IsSearchQueryAction(item))
        {
            QueryText = item.Arguments!.Trim();
            IsSearching = false;
            StatusText = string.Empty;
            return;
        }

        var result = await _processLauncher.LaunchAsync(item, CancellationToken.None);
        IsSearching = false;
        if (result.Succeeded)
        {
            if (_searchHistoryService is not null)
            {
                try
                {
                    await _searchHistoryService.RecordAsync(QueryText, CancellationToken.None);
                }
                catch
                {
                    // A history write must never turn a successful launch into an error.
                }
            }

            if (LauncherItemActionPolicy.ShouldKeepSearchOpenAfterSuccess(item))
                StatusText = $"已复制结果：{item.Arguments}";
            else
                _windowHost.Hide();
        }
        else
            StatusText = result.ErrorMessage ?? "启动失败";
    }

    [RelayCommand]
    private void Hide() => _windowHost.Hide();

    [RelayCommand]
    public async Task RevealSelectedItemAsync(CancellationToken cancellationToken = default)
    {
        var item = SelectedItem;
        if (item is null)
        {
            StatusText = "请先选择一个搜索结果";
            return;
        }

        if (!item.CanRevealLocation)
        {
            StatusText = "此结果没有可打开的位置";
            return;
        }

        IsSearching = true;
        try
        {
            var result = await _pathRevealService.RevealAsync(item.LaunchPath, cancellationToken);
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
            StatusText = $"无法打开所在位置：{exception.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    public void ReportStatus(string message)
        => StatusText = string.IsNullOrWhiteSpace(message) ? "操作失败" : message;

    private sealed class NullLauncherWindowHost : ILauncherWindowHost
    {
        public bool IsVisible => false;
        public void Toggle() { }
        public void Show() { }
        public void Hide() { }
    }
}
