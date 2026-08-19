using Avalonia.Threading;
using LightUpUI.ViewModels;
using LightUpUI.Views;

namespace LightUpUI.Services;

public sealed class LauncherWindowHost : ILauncherWindowHost
{
    private MainViewModel? _viewModel;
    private MainWindow? _window;

    public bool IsVisible => _window?.IsVisible == true;

    public void Attach(MainWindow window) => _window = window;

    public void AttachViewModel(MainViewModel viewModel) => _viewModel = viewModel;

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

        _viewModel?.ResetForActivation();
        _window.Show();
        _window.Activate();
        Dispatcher.UIThread.Post(_window.FocusQueryBox);
    }

    public void Hide()
    {
        _window?.Hide();
        _viewModel?.ResetForHide();
    }
}
