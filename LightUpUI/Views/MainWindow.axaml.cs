using System;
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
        if (!WindowChromePolicy.CanStartMoveDrag(e.Source is Control { Focusable: true }))
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
        if (TopmostButton is not null)
            TopmostIcon.Icon = Topmost
                ? FluentIcons.Common.Icon.Pin
                : FluentIcons.Common.Icon.PinOff;
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
        if (IsVisible)
            ViewModel.HideCommand.Execute(null);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
