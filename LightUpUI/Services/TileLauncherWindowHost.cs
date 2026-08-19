using Avalonia.Threading;
using LightUpUI.ViewModels;
using LightUpUI.Views;

namespace LightUpUI.Services;

public sealed class TileLauncherWindowHost(TileLauncherViewModel viewModel) : ITileLauncherWindowHost
{
    private readonly TileLauncherViewModel _viewModel = viewModel;
    private TileLauncherWindow? _window;
    private bool _loaded;

    public bool IsVisible => _window?.IsVisible == true;

    public void Attach(TileLauncherWindow window) => _window = window;

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

        if (!_loaded)
        {
            _loaded = true;
            _ = _viewModel.LoadAsync();
        }

        _window.Show();
        _window.Activate();
        Dispatcher.UIThread.Post(_window.FocusSearchBox);
    }

    public void Hide() => _window?.Hide();
}
