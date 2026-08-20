using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using LightUpUI.Models;
using LightUpUI.Presentation;
using LightUpUI.Services;
using LightUpUI.ViewModels;

namespace LightUpUI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
        : this(new MainViewModel())
    {
    }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        UpdateTopmostButton();
    }

    public void FocusQueryBox()
    {
        TryFocusQueryBox(this.FindControl<TextBox>("QueryBox"));
    }

    public static bool TryFocusQueryBox(Control? queryBox) => queryBox?.Focus() == true;

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Visual dragSurface
            || !WindowChromePolicy.CanStartMoveDrag(e.Source as Visual, dragSurface))
            return;

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void ToggleTopmost_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Topmost = WindowChromePolicy.ToggleTopmost(Topmost);
        UpdateTopmostButton();
    }

    private void Collapse_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Hide();

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Hide();

    private void ClearQuery_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ViewModel.ClearQueryCommand.Execute(null);
        FocusQueryBox();
    }

    private void Result_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control || control.DataContext is not LauncherItem item)
            return;

        if (!LauncherInteractionPolicy.ShouldSelectOnClick(e.ClickCount))
            return;

        ViewModel.SelectedItem = item;
        if (LauncherInteractionPolicy.ShouldLaunchOnClick(e.ClickCount))
            ViewModel.InvokeSelectedCommand.Execute(null);

        e.Handled = true;
    }

    private void UpdateTopmostButton()
    {
        var topmostButton = this.FindControl<Button>("TopmostButton");
        var topmostIcon = this.FindControl<FluentIcons.Avalonia.FluentIcon>("TopmostIcon");
        var topmostStatus = this.FindControl<Control>("TopmostStatus");
        if (topmostButton is null || topmostIcon is null || topmostStatus is null)
            return;

        topmostIcon.Icon = Topmost
            ? FluentIcons.Common.Icon.Pin
            : FluentIcons.Common.Icon.PinOff;
        topmostIcon.IconVariant = WindowChromePolicy.GetTopmostIconVariant(Topmost);
        ToolTip.SetTip(topmostButton, WindowChromePolicy.GetTopmostToolTip(Topmost));
        topmostStatus.IsVisible = Topmost;
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext!;

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
                ViewModel.SelectPreviousCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Down:
                ViewModel.SelectNextCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Enter:
                ViewModel.InvokeSelectedCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Escape:
                ViewModel.HideCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        if (IsVisible && WindowChromePolicy.ShouldHideOnDeactivated(Topmost, staysOpenWhenDeactivated: false))
            ViewModel.HideCommand.Execute(null);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
