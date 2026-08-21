using System;
using System.Threading.Tasks;
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

    public async Task<LaunchResult> CopyTextAsync(string text)
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is null)
                return LaunchResult.Failed("当前环境不支持剪贴板");

            var data = new DataTransfer();
            data.Add(DataTransferItem.CreateText(text));
            await clipboard.SetDataAsync(data);
            return LaunchResult.Success;
        }
        catch (Exception exception)
        {
            return LaunchResult.Failed($"复制到剪贴板失败：{exception.Message}");
        }
    }

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

    private static LauncherItem? GetContextResult(object? sender)
    {
        if (sender is not MenuItem menuItem)
            return null;

        if (menuItem.DataContext is LauncherItem item)
            return item;

        return (menuItem.Parent as ContextMenu)?.PlacementTarget?.DataContext as LauncherItem;
    }

    private LauncherItem? SelectContextResult(object? sender)
    {
        var item = GetContextResult(sender);
        if (item is not null)
            ViewModel.SelectedItem = item;

        return item;
    }

    private void ContextOpenResult_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (SelectContextResult(sender) is not null)
            ViewModel.InvokeSelectedCommand.Execute(null);

        e.Handled = true;
    }

    private async void ContextRevealResult_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (SelectContextResult(sender) is not null)
            await ViewModel.RevealSelectedItemAsync();

        e.Handled = true;
    }

    private async void ContextCopyResultPath_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var item = SelectContextResult(sender);
        if (item is null || !item.CanCopyLaunchPath)
        {
            e.Handled = true;
            return;
        }

        var result = await CopyTextAsync(item.LaunchPath);
        if (result.Succeeded)
            ViewModel.ReportStatus($"已复制路径：{item.Title}");
        else
            ViewModel.ReportStatus(result.ErrorMessage ?? "复制路径失败");

        e.Handled = true;
    }

    private async void ContextRunAsAdministrator_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (SelectContextResult(sender) is not null)
            await ViewModel.LaunchSelectedAsAdministratorAsync();

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

    private async void Window_KeyDown(object? sender, KeyEventArgs e)
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
                if ((e.KeyModifiers & KeyModifiers.Control) != 0)
                    await ViewModel.RevealSelectedItemAsync();
                else if ((e.KeyModifiers & KeyModifiers.Shift) != 0)
                    await ViewModel.CopySelectedPathAsync();
                else
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
