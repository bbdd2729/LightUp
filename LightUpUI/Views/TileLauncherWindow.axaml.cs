using System;
using System.IO;
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

    private TileLauncherViewModel ViewModel => (TileLauncherViewModel)DataContext!;

    private void Window_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files is null)
            return;

        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(path))
                ViewModel.AddItem(CreateTileItem(path));
        }

        e.Handled = true;
    }

    private void Tile_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount < 2 || sender is not Control control || control.DataContext is not TileItem item)
            return;

        ViewModel.SelectedItem = item;
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
        if (IsVisible)
            Hide();
    }

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Hide();

    private void UpdateTopmostButton()
    {
        if (TopmostButton is not null)
            TopmostIcon.Icon = Topmost
                ? FluentIcons.Common.Icon.Pin
                : FluentIcons.Common.Icon.PinOff;
    }

    private async void Add_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            Title = "选择要添加的入口"
        });

        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(path))
                ViewModel.AddItem(CreateTileItem(path));
        }
    }

    private static TileItem CreateTileItem(string path)
    {
        var extension = Path.GetExtension(path);
        var kind = Directory.Exists(path)
            ? TileItemKind.Folder
            : extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase)
                ? TileItemKind.Shortcut
                : extension.Equals(".url", StringComparison.OrdinalIgnoreCase)
                    ? TileItemKind.Url
                    : extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
                        ? TileItemKind.Application
                        : TileItemKind.File;

        return new TileItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = Path.GetFileNameWithoutExtension(path),
            TargetPath = path,
            Kind = kind
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
