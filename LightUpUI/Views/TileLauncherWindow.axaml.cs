using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using LightUpUI.Models.Tiles;
using LightUpUI.Presentation;
using LightUpUI.Services;
using LightUpUI.ViewModels;

namespace LightUpUI.Views;

public partial class TileLauncherWindow : Window
{
    private readonly TileLauncherViewModel _viewModel;

    public TileLauncherWindow()
        : this(new TileLauncherViewModel(new JsonLauncherStateStore(), new WindowsProcessLauncher()))
    {
    }

    public TileLauncherWindow(TileLauncherViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        Closed += (_, _) => viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        DragDrop.SetAllowDrop(this, true);
        UpdateTopmostButton();
        UpdateSearchWidth();
        UpdateWorkspaceLayout();
    }

    public void FocusSearchBox()
    {
        TryFocusSearchBox(this.FindControl<TextBox>("SearchBox"));
    }

    public static bool TryFocusSearchBox(Control? searchBox) => searchBox?.Focus() == true;

    public void FocusTileTitleBox()
    {
        TryFocusTileTitleBox(this.FindControl<TextBox>("TileTitleBox"));
    }

    public static bool TryFocusTileTitleBox(TextBox? titleBox)
    {
        if (titleBox?.Focus() != true)
            return false;

        titleBox.SelectAll();
        return true;
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
        else if (e.Key == Key.F2 && ViewModel.SelectedItem is not null)
        {
            FocusTileTitleBox();
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

    private void ClearSearch_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.SearchText = string.Empty;
        FocusSearchBox();
        e.Handled = true;
    }

    private static TileItem? GetContextTile(object? sender)
    {
        if (sender is not MenuItem menuItem)
            return null;

        if (menuItem.DataContext is TileItem item)
            return item;

        return (menuItem.Parent as ContextMenu)?.PlacementTarget?.DataContext as TileItem;
    }

    private TileItem? SelectContextTile(object? sender)
    {
        var item = GetContextTile(sender);
        if (item is not null)
            ViewModel.SelectedItem = item;

        return item;
    }

    private void ContextOpen_Click(object? sender, RoutedEventArgs e)
    {
        if (SelectContextTile(sender) is not null)
            ViewModel.OpenSelectedCommand.Execute(null);

        e.Handled = true;
    }

    private void ContextRename_Click(object? sender, RoutedEventArgs e)
    {
        if (SelectContextTile(sender) is not null)
            Dispatcher.UIThread.Post(FocusTileTitleBox);

        e.Handled = true;
    }

    private void ContextMove_Click(object? sender, RoutedEventArgs e)
    {
        if (SelectContextTile(sender) is not null)
            Dispatcher.UIThread.Post(() => this.FindControl<ComboBox>("MoveCategoryBox")?.Focus());

        e.Handled = true;
    }

    private async void ContextRemove_Click(object? sender, RoutedEventArgs e)
    {
        if (SelectContextTile(sender) is not null)
            await ViewModel.RemoveSelectedItemAsync();

        e.Handled = true;
    }

    private void NewCategory_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        ViewModel.AddNewCategoryCommand.Execute(null);
        e.Handled = true;
    }

    private void RenameCategory_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || !ViewModel.CanManageSelectedCategory)
            return;

        ViewModel.RenameSelectedCategoryCommand.Execute(null);
        e.Handled = true;
    }

    private void RenameTile_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || !ViewModel.CanRenameSelectedItem)
            return;

        ViewModel.RenameSelectedItemCommand.Execute(null);
        e.Handled = true;
    }

    private async void RemoveTile_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: TileItem item })
            await ViewModel.RemoveItemAsync(item);

        e.Handled = true;
    }

    private void Window_SizeChanged(object? sender, SizeChangedEventArgs e) => UpdateSearchWidth();

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TileLauncherViewModel.CategoryNavigationPlacement))
            UpdateWorkspaceLayout();
    }

    private void UpdateWorkspaceLayout()
    {
        var workspace = this.FindControl<Grid>("TileWorkspace");
        if (workspace is null || workspace.ColumnDefinitions.Count < 2)
            return;

        var layout = TileLauncherLayoutPolicy.GetWorkspaceLayout(_viewModel.CategoryNavigationPlacement);
        workspace.ColumnDefinitions[0].Width = new GridLength(layout.SidebarWidth, GridUnitType.Pixel);
        workspace.ColumnSpacing = layout.ColumnSpacing;
        workspace.RowSpacing = layout.RowSpacing;
    }

    private void UpdateSearchWidth()
    {
        var searchShell = this.FindControl<Control>("SearchShell");
        if (searchShell is not null)
            searchShell.MaxWidth = TileLauncherLayoutPolicy.GetSearchMaxWidth(Bounds.Width);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
