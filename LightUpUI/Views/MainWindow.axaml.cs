using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
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
    }

    public void FocusQueryBox() => QueryBox.Focus();

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
