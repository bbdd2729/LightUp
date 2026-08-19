using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LightUpUI.Models;
using LightUpUI.Services;
using LightUpUI.ViewModels;

namespace LightUpUI.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
        : this(new SettingsViewModel(
            new SearchLauncherSettingsStore(),
            new SearchLauncherSettings(),
            _ => { }))
    {
    }

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
