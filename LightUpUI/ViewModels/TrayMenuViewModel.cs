using System;
using CommunityToolkit.Mvvm.Input;

namespace LightUpUI.ViewModels;

public sealed partial class TrayMenuViewModel : ViewModelBase
{
    private readonly Action _openSearch;
    private readonly Action _openTiles;
    private readonly Action _openSettings;
    private readonly Action _exitApplication;

    public TrayMenuViewModel(
        Action openSearch,
        Action openTiles,
        Action openSettings,
        Action exitApplication)
    {
        _openSearch = openSearch;
        _openTiles = openTiles;
        _openSettings = openSettings;
        _exitApplication = exitApplication;
    }

    [RelayCommand]
    private void OpenSearch() => _openSearch();

    [RelayCommand]
    private void OpenTiles() => _openTiles();

    [RelayCommand]
    private void OpenSettings() => _openSettings();

    [RelayCommand]
    private void ExitApplication() => _exitApplication();
}
