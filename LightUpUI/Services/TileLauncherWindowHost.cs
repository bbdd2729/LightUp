using System.Threading.Tasks;
using Avalonia.Threading;
using LightUpUI.ViewModels;
using LightUpUI.Views;

namespace LightUpUI.Services;

public sealed class TileLauncherWindowHost(TileLauncherViewModel viewModel) : ITileLauncherWindowHost
{
    private readonly TileLauncherViewModel _viewModel = viewModel;
    private TileLauncherWindow? _window;
    private Task? _loadTask;

    public bool IsVisible => _window?.IsVisible == true;

    public void Attach(TileLauncherWindow window) => _window = window;

    public Task EnsureLoadedAsync() => _loadTask ??= _viewModel.LoadAsync();

    public void Toggle()
    {
        if (IsVisible)
            Hide();
        else
            Show();
    }

    public void Show()
    {
        if (_window is null)
            return;

        _ = EnsureLoadedAsync();

        _window.Show();
        _window.Activate();
        Dispatcher.UIThread.Post(_window.FocusSearchBox);
    }

    public void Hide() => _window?.Hide();
}
