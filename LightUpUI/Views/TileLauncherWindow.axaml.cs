using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using LightUpUI.Models.Tiles;
using LightUpUI.Presentation;
using LightUpUI.Services;
using LightUpUI.ViewModels;

namespace LightUpUI.Views;

public partial class TileLauncherWindow : Window
{
    public TileLauncherWindow()
        : this(new TileLauncherViewModel(new JsonLauncherStateStore(), new WindowsProcessLauncher()))
    {
    }

    public TileLauncherWindow(TileLauncherViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        DragDrop.SetAllowDrop(this, true);
        UpdateTopmostButton();
    }

    public void FocusSearchBox()
    {
        TryFocusSearchBox(this.FindControl<TextBox>("SearchBox"));
    }

    public static bool TryFocusSearchBox(Control? searchBox) => searchBox?.Focus() == true;

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!WindowChromePolicy.CanStartMoveDrag(e.Source as Visual, (Visual)sender!))
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

    private TileLauncherViewModel ViewModel => (TileLauncherViewModel)DataContext!;

    private void Window_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private async void Window_Drop(object? sender, DragEventArgs e)
    {
        try
        {
            var files = e.DataTransfer.TryGetFiles();
            if (files is not null)
                await AddFilesAsync(files);
        }
        catch (Exception exception)
        {
            ViewModel.ReportError($"添加入口失败：{exception.Message}");
        }
        finally
        {
            e.Handled = true;
        }
    }

    private void Tile_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control || control.DataContext is not TileItem item)
            return;

        if (!LauncherInteractionPolicy.ShouldSelectOnClick(e.ClickCount))
            return;

        ViewModel.SelectedItem = item;
        if (LauncherInteractionPolicy.ShouldLaunchOnClick(e.ClickCount))
            ViewModel.OpenSelectedCommand.Execute(null);

        e.Handled = true;
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            ViewModel.OpenSelectedCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        if (IsVisible && WindowChromePolicy.ShouldHideOnDeactivated(Topmost, staysOpenWhenDeactivated: true))
            Hide();
    }

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Hide();

    private void UpdateTopmostButton()
    {
        if (TopmostButton is not null)
        {
            TopmostIcon.Icon = Topmost
                ? FluentIcons.Common.Icon.Pin
                : FluentIcons.Common.Icon.PinOff;
            TopmostIcon.IconVariant = WindowChromePolicy.GetTopmostIconVariant(Topmost);
            ToolTip.SetTip(TopmostButton, WindowChromePolicy.GetTopmostToolTip(Topmost));
            TopmostStatus.IsVisible = Topmost;
        }
    }

    private async void Add_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = true,
                Title = "选择要添加的入口"
            });

            await AddFilesAsync(files);
        }
        catch (Exception exception)
        {
            ViewModel.ReportError($"添加入口失败：{exception.Message}");
        }
    }

    private async Task AddFilesAsync(IEnumerable<IStorageItem> files)
    {
        var items = new List<TileItem>();
        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(path))
                items.Add(TileItemFactory.Create(path));
        }

        await ViewModel.AddItemsAsync(items);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
