using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LightUpUI.Services;
using LightUpUI.ViewModels;

namespace LightUpUI.Views;

public partial class TrayMenuWindow : Window
{
    public TrayMenuWindow()
        : this(new TrayMenuViewModel(() => { }, () => { }, () => { }, () => { }))
    {
    }

    public TrayMenuWindow(TrayMenuViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public void Toggle()
    {
        if (IsVisible)
        {
            Hide();
            return;
        }

        ShowNearPrimaryTaskbar();
        Activate();
    }

    private void ShowNearPrimaryTaskbar()
    {
        Show();
        var workingArea = Screens.Primary?.WorkingArea;
        if (workingArea is not { } area)
            return;

        var width = (int)Math.Ceiling(FrameSize?.Width ?? ClientSize.Width);
        var height = (int)Math.Ceiling(FrameSize?.Height ?? ClientSize.Height);
        Position = TrayMenuPlacementPolicy.GetPosition(area, new PixelSize(width, height));
    }

    private void Window_Deactivated(object? sender, System.EventArgs e) => Hide();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
