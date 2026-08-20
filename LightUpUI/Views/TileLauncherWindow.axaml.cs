using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
        AddHandler(DragDrop.DragOverEvent, Category_DragOver, RoutingStrategies.Bubble);
        AddHandler(DragDrop.DragLeaveEvent, Category_DragLeave, RoutingStrategies.Bubble);
        AddHandler(DragDrop.DropEvent, Category_Drop, RoutingStrategies.Bubble);
        AddHandler(DragDrop.DragOverEvent, Tile_DragOver, RoutingStrategies.Bubble);
        AddHandler(DragDrop.DragLeaveEvent, Tile_DragLeave, RoutingStrategies.Bubble);
        AddHandler(DragDrop.DropEvent, Tile_Drop, RoutingStrategies.Bubble);
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

    public void FocusTileNotesBox()
    {
        TryFocusTileNotesBox(this.FindControl<TextBox>("TileNotesBox"));
    }

    public static bool TryFocusTileNotesBox(TextBox? notesBox)
    {
        if (notesBox?.Focus() != true)
            return false;

        notesBox.SelectAll();
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
        var kind = TileDropPolicy.Classify(
            e.DataTransfer.Contains(DataFormat.File),
            e.DataTransfer.TryGetText());
        if (kind == TileExternalDropKind.None)
            return;

        UpdateExternalDropFeedback(kind);
        e.DragEffects = kind == TileExternalDropKind.InvalidText
            ? DragDropEffects.None
            : DragDropEffects.Copy;
        e.Handled = true;
    }

    private void Window_DragLeave(object? sender, DragEventArgs e) => ClearExternalDropFeedback();

    private async void Window_Drop(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        var text = e.DataTransfer.TryGetText();
        var kind = TileDropPolicy.Classify(files is not null, text);
        ClearExternalDropFeedback();

        if (kind == TileExternalDropKind.File && files is not null)
        {
            try
            {
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

            return;
        }

        if (kind == TileExternalDropKind.None)
            return;

        if (kind == TileExternalDropKind.InvalidText)
        {
            ViewModel.ReportError(TileDropPolicy.GetFeedback(kind));
            e.Handled = true;
            return;
        }

        if (!TileItemFactory.TryCreateUrl(text, out var item))
            return;

        try
        {
            await ViewModel.AddItemsAsync([item]);
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
        if (TileLauncherKeyboardPolicy.ShouldRemoveSelectedItem(e.Key, IsTextEditing()))
        {
            ViewModel.RemoveSelectedItemCommand.Execute(null);
            e.Handled = true;
        }
        else
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

    private bool IsTextEditing()
        => FocusManager?.GetFocusedElement() is TextBox;

    private void ClearSearch_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.SearchText = string.Empty;
        FocusSearchBox();
        e.Handled = true;
    }

    private async void TileDragHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control
            || control.DataContext is not TileItem item
            || !e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
            return;

        ViewModel.SelectedItem = item;
        var payload = new DataTransfer();
        payload.Add(DataTransferItem.CreateText(Presentation.TileDragPayload.Create(item.Id)));
        e.Handled = true;
        await DragDrop.DoDragDropAsync(e, payload, DragDropEffects.Move);
    }

    private static bool TryGetDraggedTileId(DragEventArgs e, out string tileId)
        => Presentation.TileDragPayload.TryParse(e.DataTransfer.TryGetText(), out tileId);

    private static TileCategory? GetDropCategory(object? sender)
        => (sender as Control)?.DataContext as TileCategory;

    private static TileItem? GetDropTile(object? sender)
        => (sender as Control)?.DataContext as TileItem;

    private static Control? FindDropTarget<T>(object? source, string className)
        where T : class
    {
        for (var visual = source as Visual; visual is not null; visual = visual.GetVisualParent())
        {
            if (visual is Control control
                && control.DataContext is T
                && control.Classes.Contains(className))
                return control;
        }

        return null;
    }

    private static Control? FindCategoryDropTarget(object? source)
        => FindDropTarget<TileCategory>(source, "category-item")
           ?? FindDropTarget<TileCategory>(source, "category-top-item");

    private void Category_DragOver(object? sender, DragEventArgs e)
    {
        var target = FindCategoryDropTarget(e.Source);
        var category = GetDropCategory(target);
        if (category is null || !TryGetDraggedTileId(e, out _))
            return;

        target!.Classes.Add("category-drop-target");
        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void Category_DragLeave(object? sender, DragEventArgs e)
    {
        FindCategoryDropTarget(e.Source)?.Classes.Remove("category-drop-target");
    }

    private async void Category_Drop(object? sender, DragEventArgs e)
    {
        var target = FindCategoryDropTarget(e.Source);
        if (GetDropCategory(target) is not TileCategory category
            || !TryGetDraggedTileId(e, out var tileId))
            return;

        try
        {
            await ViewModel.MoveTileByIdToCategoryAsync(tileId, category);
        }
        finally
        {
            target?.Classes.Remove("category-drop-target");
            e.Handled = true;
        }
    }

    private void UpdateExternalDropFeedback(TileExternalDropKind kind)
    {
        var overlay = this.FindControl<Border>("ExternalDropOverlay");
        var message = this.FindControl<TextBlock>("ExternalDropMessage");
        if (overlay is not null)
        {
            overlay.IsVisible = kind != TileExternalDropKind.None;
            SetClass(overlay, "drop-invalid", kind == TileExternalDropKind.InvalidText);
        }

        if (message is not null)
            message.Text = TileDropPolicy.GetFeedback(kind);
    }

    private void ClearExternalDropFeedback() => UpdateExternalDropFeedback(TileExternalDropKind.None);

    private static void SetClass(Control control, string className, bool enabled)
    {
        if (enabled)
            control.Classes.Add(className);
        else
            control.Classes.Remove(className);
    }

    private void Tile_DragOver(object? sender, DragEventArgs e)
    {
        var target = FindDropTarget<TileItem>(e.Source, "tile-card");
        if (target is null || !TryGetDraggedTileId(e, out _))
            return;

        var insertAfterTarget = e.GetPosition(target).Y >= target.Bounds.Height / 2;
        SetClass(target, "tile-drop-before", !insertAfterTarget);
        SetClass(target, "tile-drop-after", insertAfterTarget);
        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void Tile_DragLeave(object? sender, DragEventArgs e)
    {
        if (FindDropTarget<TileItem>(e.Source, "tile-card") is { } target)
            ClearTileDropFeedback(target);
    }

    private async void Tile_Drop(object? sender, DragEventArgs e)
    {
        var target = FindDropTarget<TileItem>(e.Source, "tile-card");
        if (target is null
            || GetDropTile(target) is not TileItem targetItem
            || !TryGetDraggedTileId(e, out var tileId))
            return;

        try
        {
            var dropPosition = e.GetPosition(target);
            var insertAfterTarget = dropPosition.Y >= target.Bounds.Height / 2;
            await ViewModel.MoveTileByIdWithinCategoryAsync(tileId, targetItem.Id, insertAfterTarget);
        }
        finally
        {
            ClearTileDropFeedback(target);
            e.Handled = true;
        }
    }

    private static void ClearTileDropFeedback(Control target)
    {
        target.Classes.Remove("tile-drop-before");
        target.Classes.Remove("tile-drop-after");
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

    private void ContextNotes_Click(object? sender, RoutedEventArgs e)
    {
        if (SelectContextTile(sender) is not null)
            Dispatcher.UIThread.Post(FocusTileNotesBox);

        e.Handled = true;
    }

    private async void ContextReveal_Click(object? sender, RoutedEventArgs e)
    {
        if (SelectContextTile(sender) is not null)
            await ViewModel.OpenContainingFolderAsync();

        e.Handled = true;
    }

    private async void ContextRetarget_Click(object? sender, RoutedEventArgs e)
    {
        var item = SelectContextTile(sender);
        if (item is not null)
            await RetargetItemAsync(item);

        e.Handled = true;
    }

    private async Task RetargetItemAsync(TileItem item)
    {
        try
        {
            string? path;
            if (item.Kind == TileItemKind.Folder)
            {
                var folder = (await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    AllowMultiple = false,
                    Title = "选择新的文件夹目标"
                })).FirstOrDefault();
                path = folder?.TryGetLocalPath();
            }
            else
            {
                var file = (await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    AllowMultiple = false,
                    Title = "选择新的入口目标"
                })).FirstOrDefault();
                path = file?.TryGetLocalPath();
            }

            if (string.IsNullOrWhiteSpace(path))
                return;

            await ViewModel.RetargetTileAsync(item.Id, TileItemFactory.Create(path));
        }
        catch (Exception exception)
        {
            ViewModel.ReportError($"重新选择目标失败：{exception.Message}");
        }
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

    private void SaveNotes_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || !ViewModel.CanEditSelectedItemNotes)
            return;

        ViewModel.SaveSelectedItemNotesCommand.Execute(null);
        e.Handled = true;
    }

    private async void RemoveTile_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: TileItem item })
            await ViewModel.RemoveTileByIdAsync(item.Id);

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
