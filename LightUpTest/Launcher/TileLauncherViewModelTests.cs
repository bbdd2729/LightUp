using LightUpUI.Models.Tiles;
using LightUpUI.Services;
using LightUpUI.ViewModels;

namespace LightUpTest.Launcher;

public sealed class TileLauncherViewModelTests
{
    [Fact]
    public async Task LoadAsync_selects_the_saved_category_and_exposes_its_items()
    {
        var state = new TileLauncherState
        {
            SelectedCategoryId = "dev",
            Categories =
            [
                new TileCategory { Id = "all", Name = "全部" },
                new TileCategory
                {
                    Id = "dev",
                    Name = "开发",
                    Items = [new TileItem { Id = "code", Title = "Code", TargetPath = "code.exe" }]
                }
            ]
        };
        var viewModel = new TileLauncherViewModel(new FakeStateStore(state), new FakeProcessLauncher());

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal("dev", viewModel.SelectedCategory?.Id);
        Assert.Single(viewModel.VisibleItems);
        Assert.Equal("code", viewModel.VisibleItems[0].Id);
    }

    [Fact]
    public void Category_navigation_flags_follow_the_configured_placement()
    {
        var viewModel = new TileLauncherViewModel(
            new FakeStateStore(CreateDefaultState()),
            new FakeProcessLauncher(),
            categoryNavigationPlacement: CategoryNavigationPlacement.Top);

        Assert.True(viewModel.IsTopNavigation);
        Assert.False(viewModel.IsLeftNavigation);

        viewModel.CategoryNavigationPlacement = CategoryNavigationPlacement.Left;

        Assert.True(viewModel.IsLeftNavigation);
        Assert.False(viewModel.IsTopNavigation);
    }

    [Fact]
    public void Category_navigation_defaults_to_left_for_an_unknown_configuration_value()
    {
        var viewModel = new TileLauncherViewModel(
            new FakeStateStore(CreateDefaultState()),
            new FakeProcessLauncher(),
            categoryNavigationPlacement: (CategoryNavigationPlacement)42);

        Assert.Equal(CategoryNavigationPlacement.Left, viewModel.CategoryNavigationPlacement);
        Assert.True(viewModel.IsLeftNavigation);
        Assert.False(viewModel.IsTopNavigation);
    }

    [Fact]
    public async Task AddItem_adds_to_the_current_category_and_requests_a_save()
    {
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories = [new TileCategory { Id = "all", Name = "全部" }]
        });
        var viewModel = new TileLauncherViewModel(store, new FakeProcessLauncher());
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.AddItem(new TileItem { Id = "notes", Title = "Notes", TargetPath = "notes.exe" });

        Assert.Single(viewModel.VisibleItems);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task AddCategory_creates_selects_and_persists_a_named_category()
    {
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories = [new TileCategory { Id = "all", Name = "全部" }]
        });
        var viewModel = new TileLauncherViewModel(store, new FakeProcessLauncher());
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.AddCategory("工作");

        Assert.Equal(2, viewModel.Categories.Count);
        Assert.Equal("工作", viewModel.SelectedCategory?.Name);
        Assert.Equal(viewModel.SelectedCategory?.Id, store.State.SelectedCategoryId);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task RenameSelectedCategory_updates_the_name_and_persists_it()
    {
        var store = new FakeStateStore(new TileLauncherState
        {
            SelectedCategoryId = "work",
            Categories =
            [
                new TileCategory { Id = "all", Name = "全部" },
                new TileCategory { Id = "work", Name = "工作" }
            ]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);
        viewModel.EditedCategoryName = "  专注工作  ";

        await viewModel.RenameSelectedCategoryAsync(TestContext.Current.CancellationToken);

        Assert.Equal("专注工作", viewModel.SelectedCategory?.Name);
        Assert.Equal("专注工作", store.State.Categories.Single(category => category.Id == "work").Name);
        Assert.Equal(1, store.SaveCount);
        Assert.Contains("已重命名", viewModel.StatusText);
    }

    [Fact]
    public async Task RenameSelectedCategory_keeps_the_default_category_protected()
    {
        var store = new FakeStateStore(CreateDefaultState());
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);
        viewModel.EditedCategoryName = "别的名称";

        await viewModel.RenameSelectedCategoryAsync(TestContext.Current.CancellationToken);

        Assert.False(viewModel.CanManageSelectedCategory);
        Assert.Equal("全部", viewModel.SelectedCategory?.Name);
        Assert.Equal(0, store.SaveCount);
        Assert.Contains("不能重命名", viewModel.StatusText);
    }

    [Fact]
    public async Task RemoveSelectedCategory_moves_its_unique_tiles_to_default_and_persists_once()
    {
        var allItem = CreateTile("existing");
        var workItem = CreateTile("work");
        var duplicateItem = CreateTile("existing");
        var store = new FakeStateStore(new TileLauncherState
        {
            SelectedCategoryId = "work",
            Categories =
            [
                new TileCategory { Id = "all", Name = "全部", Items = [allItem] },
                new TileCategory { Id = "work", Name = "工作", Items = [workItem, duplicateItem] }
            ]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);

        await viewModel.RemoveSelectedCategoryAsync(TestContext.Current.CancellationToken);

        Assert.Single(viewModel.Categories);
        Assert.Equal("all", viewModel.SelectedCategory?.Id);
        Assert.Equal(["existing", "work"], viewModel.SelectedCategory!.Items.Select(item => item.Id));
        Assert.Equal("all", store.State.SelectedCategoryId);
        Assert.Equal(1, store.SaveCount);
        Assert.Contains("已移至“全部”", viewModel.StatusText);
    }

    [Fact]
    public async Task RemoveSelectedCategory_keeps_the_default_category_protected()
    {
        var store = new FakeStateStore(CreateDefaultState());
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);

        await viewModel.RemoveSelectedCategoryAsync(TestContext.Current.CancellationToken);

        Assert.Single(viewModel.Categories);
        Assert.Equal(0, store.SaveCount);
        Assert.Contains("不能删除", viewModel.StatusText);
    }

    [Fact]
    public async Task MoveItemAsync_moves_the_item_to_the_destination_and_persists_once()
    {
        var first = CreateTile("first");
        var itemToMove = CreateTile("move");
        var last = CreateTile("last");
        first.SortOrder = 4;
        itemToMove.SortOrder = 8;
        last.SortOrder = 12;
        var store = new FakeStateStore(new TileLauncherState
        {
            SelectedCategoryId = "work",
            Categories =
            [
                new TileCategory { Id = "all", Name = "全部" },
                new TileCategory { Id = "work", Name = "工作", Items = [first, itemToMove, last] },
                new TileCategory { Id = "archive", Name = "归档" }
            ]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);
        var destination = viewModel.Categories.Single(category => category.Id == "archive");

        await viewModel.MoveItemAsync(itemToMove, destination, TestContext.Current.CancellationToken);

        var source = viewModel.Categories.Single(category => category.Id == "work");
        Assert.Equal(["first", "last"], source.Items.Select(item => item.Id));
        Assert.Equal([0, 1], source.Items.Select(item => item.SortOrder));
        Assert.Equal(["move"], destination.Items.Select(item => item.Id));
        Assert.Equal(0, destination.Items[0].SortOrder);
        Assert.Equal(1, store.SaveCount);
        Assert.Contains("已移动", viewModel.StatusText);
    }

    [Fact]
    public async Task MoveItemAsync_rejects_a_duplicate_target_without_mutating_or_saving()
    {
        var itemToMove = CreateTile("same");
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories =
            [
                new TileCategory { Id = "all", Name = "全部" },
                new TileCategory { Id = "work", Name = "工作", Items = [itemToMove] },
                new TileCategory { Id = "archive", Name = "归档", Items = [CreateTile("same")] }
            ]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);
        var destination = viewModel.Categories.Single(category => category.Id == "archive");

        await viewModel.MoveItemAsync(itemToMove, destination, TestContext.Current.CancellationToken);

        Assert.Single(viewModel.Categories.Single(category => category.Id == "work").Items);
        Assert.Single(destination.Items);
        Assert.Equal(0, store.SaveCount);
        Assert.Contains("已存在", viewModel.StatusText);
    }

    [Fact]
    public async Task MoveItemAsync_rejects_the_current_category_without_saving()
    {
        var item = CreateTile("current");
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories =
            [
                new TileCategory { Id = "all", Name = "全部" },
                new TileCategory { Id = "work", Name = "工作", Items = [item] }
            ]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);
        var source = viewModel.Categories.Single(category => category.Id == "work");

        await viewModel.MoveItemAsync(item, source, TestContext.Current.CancellationToken);

        Assert.Single(source.Items);
        Assert.Equal(0, store.SaveCount);
        Assert.Contains("当前分类", viewModel.StatusText);
    }

    [Fact]
    public async Task RenameSelectedItemAsync_updates_the_title_and_persists_once()
    {
        var item = CreateTile("notes");
        item.Title = "旧名称";
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories = [new TileCategory { Id = "all", Name = "全部", Items = [item] }]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);
        viewModel.SelectedItem = item;
        viewModel.EditedTileTitle = "  我的笔记  ";

        await viewModel.RenameSelectedItemAsync(TestContext.Current.CancellationToken);

        Assert.Equal("我的笔记", item.Title);
        Assert.Equal("我的笔记", viewModel.VisibleItems.Single().Title);
        Assert.Equal(1, store.SaveCount);
        Assert.Contains("已重命名", viewModel.StatusText);
    }

    [Fact]
    public async Task RenameSelectedItemAsync_rejects_an_empty_title_without_saving()
    {
        var item = CreateTile("notes");
        item.Title = "保留名称";
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories = [new TileCategory { Id = "all", Name = "全部", Items = [item] }]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);
        viewModel.SelectedItem = item;
        viewModel.EditedTileTitle = "   ";

        await viewModel.RenameSelectedItemAsync(TestContext.Current.CancellationToken);

        Assert.Equal("保留名称", item.Title);
        Assert.Equal(0, store.SaveCount);
        Assert.Contains("不能为空", viewModel.StatusText);
    }

    [Fact]
    public async Task RemoveSelectedItemAsync_can_restore_the_item_at_its_original_position()
    {
        var first = CreateTile("first");
        var removed = CreateTile("remove");
        var last = CreateTile("last");
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories = [new TileCategory { Id = "all", Name = "全部", Items = [first, removed, last] }]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);
        viewModel.SelectedItem = removed;

        await viewModel.RemoveSelectedItemAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["first", "last"], viewModel.VisibleItems.Select(item => item.Id));
        Assert.True(viewModel.CanUndoLastRemoval);
        Assert.Equal(1, store.SaveCount);

        await viewModel.UndoLastRemovalAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["first", "remove", "last"], viewModel.VisibleItems.Select(item => item.Id));
        Assert.Equal([0, 1, 2], viewModel.VisibleItems.Select(item => item.SortOrder));
        Assert.False(viewModel.CanUndoLastRemoval);
        Assert.Equal(2, store.SaveCount);
        Assert.Contains("已恢复", viewModel.StatusText);
    }

    [Fact]
    public async Task RemoveTileByIdAsync_selects_and_removes_the_matching_item_through_the_shared_command_path()
    {
        var item = CreateTile("remove-by-id");
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories = [new TileCategory { Id = "all", Name = "全部", Items = [item] }]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);

        await viewModel.RemoveTileByIdAsync(item.Id, TestContext.Current.CancellationToken);

        Assert.Empty(viewModel.VisibleItems);
        Assert.True(viewModel.CanUndoLastRemoval);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task UndoLastRemovalAsync_rejects_an_item_that_was_readded_at_the_same_path()
    {
        var removed = CreateTile("remove");
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories = [new TileCategory { Id = "all", Name = "全部", Items = [removed] }]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);
        viewModel.SelectedItem = removed;
        await viewModel.RemoveSelectedItemAsync(TestContext.Current.CancellationToken);
        await viewModel.AddItemsAsync([CreateTile("remove")], TestContext.Current.CancellationToken);

        await viewModel.UndoLastRemovalAsync(TestContext.Current.CancellationToken);

        Assert.Single(viewModel.VisibleItems);
        Assert.True(viewModel.CanUndoLastRemoval);
        Assert.Equal(2, store.SaveCount);
        Assert.Contains("已存在", viewModel.StatusText);
    }

    [Fact]
    public async Task OpenContainingFolderAsync_reveals_the_selected_entry_and_reports_success()
    {
        var item = CreateTile("C:\\Tools\\notes.exe");
        var revealService = new FakePathRevealService(LaunchResult.Success);
        var viewModel = new TileLauncherViewModel(
            new FakeStateStore(new TileLauncherState
            {
                Categories = [new TileCategory { Id = "all", Name = "全部", Items = [item] }]
            }),
            new FakeProcessLauncher(),
            pathRevealService: revealService);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SelectedItem = item;

        await viewModel.OpenContainingFolderAsync(TestContext.Current.CancellationToken);

        Assert.Equal(item.TargetPath, revealService.LastPath);
        Assert.Contains("已打开所在位置", viewModel.StatusText);
    }

    [Fact]
    public async Task OpenContainingFolderAsync_reports_a_reveal_failure_without_throwing()
    {
        var item = CreateTile("missing.exe");
        var viewModel = new TileLauncherViewModel(
            new FakeStateStore(new TileLauncherState
            {
                Categories = [new TileCategory { Id = "all", Name = "全部", Items = [item] }]
            }),
            new FakeProcessLauncher(),
            pathRevealService: new FakePathRevealService(LaunchResult.Failed("目标不存在")));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SelectedItem = item;

        await viewModel.OpenContainingFolderAsync(TestContext.Current.CancellationToken);

        Assert.Contains("目标不存在", viewModel.StatusText);
        Assert.False(viewModel.IsOpening);
    }

    [Fact]
    public async Task SaveSelectedItemNotesAsync_persists_trimmed_notes_and_updates_the_editor()
    {
        var item = CreateTile("notes");
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories = [new TileCategory { Id = "all", Name = "全部", Items = [item] }]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);
        viewModel.SelectedItem = item;
        viewModel.EditedTileNotes = "  重要资料  ";

        await viewModel.SaveSelectedItemNotesAsync(TestContext.Current.CancellationToken);

        Assert.Equal("重要资料", item.Notes);
        Assert.Equal("重要资料", viewModel.EditedTileNotes);
        Assert.Equal(1, store.SaveCount);
        Assert.Contains("已保存备注", viewModel.StatusText);
    }

    [Fact]
    public async Task SaveSelectedItemNotesAsync_clears_whitespace_only_notes()
    {
        var item = CreateTile("notes");
        item.Notes = "旧备注";
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories = [new TileCategory { Id = "all", Name = "全部", Items = [item] }]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);
        viewModel.SelectedItem = item;
        viewModel.EditedTileNotes = "   ";

        await viewModel.SaveSelectedItemNotesAsync(TestContext.Current.CancellationToken);

        Assert.Null(item.Notes);
        Assert.Equal(string.Empty, viewModel.EditedTileNotes);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task MoveSelectedCategoryAsync_moves_custom_categories_and_reindexes_them()
    {
        var store = new FakeStateStore(new TileLauncherState
        {
            SelectedCategoryId = "archive",
            Categories =
            [
                new TileCategory { Id = "all", Name = "全部", SortOrder = 20 },
                new TileCategory { Id = "work", Name = "工作", SortOrder = 30 },
                new TileCategory { Id = "archive", Name = "归档", SortOrder = 40 }
            ]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);

        await viewModel.MoveSelectedCategoryAsync(-1, TestContext.Current.CancellationToken);

        Assert.Equal(["all", "archive", "work"], viewModel.Categories.Select(category => category.Id));
        Assert.Equal([0, 1, 2], viewModel.Categories.Select(category => category.SortOrder));
        Assert.Equal("archive", viewModel.SelectedCategory?.Id);
        Assert.Equal(1, store.SaveCount);
        Assert.Contains("已上移", viewModel.StatusText);
    }

    [Fact]
    public async Task MoveSelectedCategoryAsync_keeps_all_fixed_and_rejects_boundary_moves()
    {
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories =
            [
                new TileCategory { Id = "all", Name = "全部" },
                new TileCategory { Id = "work", Name = "工作" }
            ]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);

        await viewModel.MoveSelectedCategoryAsync(-1, TestContext.Current.CancellationToken);
        Assert.Equal(["all", "work"], viewModel.Categories.Select(category => category.Id));
        Assert.Equal(0, store.SaveCount);

        viewModel.SelectedCategory = viewModel.Categories.Single(category => category.Id == "work");
        await viewModel.MoveSelectedCategoryAsync(1, TestContext.Current.CancellationToken);

        Assert.Equal(["all", "work"], viewModel.Categories.Select(category => category.Id));
        Assert.Equal(1, store.SaveCount);
        Assert.Contains("已经是最后", viewModel.StatusText);
    }

    [Fact]
    public async Task MoveSelectedItemWithinCategoryAsync_reorders_the_item_and_persists_once()
    {
        var first = CreateTile("first");
        var selected = CreateTile("selected");
        var last = CreateTile("last");
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories = [new TileCategory { Id = "all", Name = "全部", Items = [first, selected, last] }]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);
        viewModel.SelectedItem = selected;

        await viewModel.MoveSelectedItemWithinCategoryAsync(-1, TestContext.Current.CancellationToken);

        Assert.Equal(["selected", "first", "last"], viewModel.VisibleItems.Select(item => item.Id));
        Assert.Equal([0, 1, 2], viewModel.VisibleItems.Select(item => item.SortOrder));
        Assert.Same(selected, viewModel.SelectedItem);
        Assert.Equal(1, store.SaveCount);
        Assert.Contains("已上移", viewModel.StatusText);
    }

    [Fact]
    public async Task MoveSelectedItemWithinCategoryAsync_rejects_a_boundary_move_without_saving()
    {
        var item = CreateTile("first");
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories = [new TileCategory { Id = "all", Name = "全部", Items = [item] }]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);
        viewModel.SelectedItem = item;

        await viewModel.MoveSelectedItemWithinCategoryAsync(-1, TestContext.Current.CancellationToken);

        Assert.Single(viewModel.VisibleItems);
        Assert.False(viewModel.CanMoveSelectedItemUp);
        Assert.False(viewModel.CanMoveSelectedItemDown);
        Assert.Equal(0, store.SaveCount);
        Assert.Contains("已经是第一个", viewModel.StatusText);
    }

    [Fact]
    public async Task MoveTileByIdToCategoryAsync_moves_only_the_dragged_tile()
    {
        var first = CreateTile("first");
        var dragged = CreateTile("dragged");
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories =
            [
                new TileCategory { Id = "all", Name = "全部" },
                new TileCategory { Id = "work", Name = "工作", Items = [first, dragged] },
                new TileCategory { Id = "archive", Name = "归档" }
            ]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);
        var destination = viewModel.Categories.Single(category => category.Id == "archive");

        await viewModel.MoveTileByIdToCategoryAsync(dragged.Id, destination, TestContext.Current.CancellationToken);

        Assert.Equal(["first"], viewModel.Categories.Single(category => category.Id == "work").Items.Select(item => item.Id));
        Assert.Equal(["dragged"], destination.Items.Select(item => item.Id));
        Assert.Equal(1, store.SaveCount);
        Assert.Contains("已移动", viewModel.StatusText);
    }

    [Fact]
    public async Task MoveTileByIdWithinCategoryAsync_reorders_after_the_drop_target()
    {
        var first = CreateTile("first");
        var dragged = CreateTile("dragged");
        var target = CreateTile("target");
        var last = CreateTile("last");
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories = [new TileCategory { Id = "all", Name = "全部", Items = [first, dragged, target, last] }]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);

        await viewModel.MoveTileByIdWithinCategoryAsync(
            dragged.Id,
            target.Id,
            insertAfterTarget: true,
            TestContext.Current.CancellationToken);

        Assert.Equal(["first", "target", "dragged", "last"], viewModel.VisibleItems.Select(item => item.Id));
        Assert.Equal([0, 1, 2, 3], viewModel.VisibleItems.Select(item => item.SortOrder));
        Assert.Equal("dragged", viewModel.SelectedItem?.Id);
        Assert.Equal(1, store.SaveCount);
        Assert.Contains("已调整顺序", viewModel.StatusText);
    }

    [Fact]
    public async Task MoveTileByIdWithinCategoryAsync_rejects_a_self_drop_without_saving()
    {
        var item = CreateTile("self");
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories = [new TileCategory { Id = "all", Name = "全部", Items = [item] }]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, TestContext.Current.CancellationToken);

        await viewModel.MoveTileByIdWithinCategoryAsync(
            item.Id,
            item.Id,
            insertAfterTarget: false,
            TestContext.Current.CancellationToken);

        Assert.Single(viewModel.VisibleItems);
        Assert.Equal(0, store.SaveCount);
        Assert.Contains("无需调整", viewModel.StatusText);
    }

    [Fact]
    public async Task Selecting_a_category_persists_it_as_the_next_active_category()
    {
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories =
            [
                new TileCategory { Id = "all", Name = "全部" },
                new TileCategory { Id = "work", Name = "工作" }
            ]
        });
        var viewModel = new TileLauncherViewModel(store, new FakeProcessLauncher());
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        viewModel.SelectedCategory = viewModel.Categories[1];

        Assert.Equal("work", store.State.SelectedCategoryId);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task AddItemsAsync_updates_items_before_a_deferred_save_finishes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new DeferredSaveStateStore(CreateDefaultState());
        var viewModel = await CreateLoadedViewModelAsync(store, cancellationToken);

        var addTask = viewModel.AddItemsAsync(
            [CreateTile("first"), CreateTile("second")],
            cancellationToken);

        Assert.Equal(2, viewModel.VisibleItems.Count);
        Assert.True(viewModel.IsSaving);
        Assert.True(viewModel.CanEdit);

        store.CompleteSave();
        await addTask.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);

        Assert.False(viewModel.IsSaving);
        Assert.Contains("2", viewModel.StatusText);
    }

    [Fact]
    public async Task AddItemsAsync_persists_a_batch_once()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeStateStore(CreateDefaultState());
        var viewModel = await CreateLoadedViewModelAsync(store, cancellationToken);

        await viewModel.AddItemsAsync(
            [CreateTile("first"), CreateTile("second")],
            cancellationToken);

        Assert.Equal(1, store.SaveCount);
        Assert.Equal(2, viewModel.VisibleItems.Count);
    }

    [Fact]
    public async Task AddItemsAsync_reports_save_failure_without_losing_the_added_items()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FailingSaveStateStore(CreateDefaultState());
        var viewModel = await CreateLoadedViewModelAsync(store, cancellationToken);

        await viewModel.AddItemsAsync([CreateTile("broken")], cancellationToken);

        Assert.Single(viewModel.VisibleItems);
        Assert.Contains("保存失败", viewModel.StatusText);
        Assert.False(viewModel.IsSaving);
    }

    [Fact]
    public async Task AddItemsAsync_rejects_duplicate_paths_without_saving()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeStateStore(new TileLauncherState
        {
            Categories =
            [
                new TileCategory
                {
                    Id = "all",
                    Name = "全部",
                    Items = [CreateTile("same")]
                }
            ]
        });
        var viewModel = await CreateLoadedViewModelAsync(store, cancellationToken);

        await viewModel.AddItemsAsync([CreateTile("same")], cancellationToken);

        Assert.Single(viewModel.VisibleItems);
        Assert.Equal(0, store.SaveCount);
        Assert.Contains("已存在", viewModel.StatusText);
    }

    [Fact]
    public async Task Empty_state_describes_the_current_filter_and_clears_after_an_add()
    {
        var viewModel = await CreateLoadedViewModelAsync(
            new FakeStateStore(CreateDefaultState()),
            TestContext.Current.CancellationToken);

        Assert.False(viewModel.HasVisibleItems);
        Assert.Equal("当前分类还没有磁贴", viewModel.EmptyStateText);

        viewModel.SearchText = "missing";
        Assert.Equal("当前分类没有匹配的磁贴", viewModel.EmptyStateText);

        await viewModel.AddItemsAsync([CreateTile("missing")], TestContext.Current.CancellationToken);

        Assert.True(viewModel.HasVisibleItems);
        Assert.DoesNotContain("没有", viewModel.EmptyStateText);
    }

    [Fact]
    public async Task Removing_the_selected_item_clears_selection_and_updates_empty_state()
    {
        var item = CreateTile("remove-me");
        var state = CreateDefaultState();
        state.Categories[0].Items.Add(item);
        var viewModel = await CreateLoadedViewModelAsync(
            new FakeStateStore(state),
            TestContext.Current.CancellationToken);

        viewModel.SelectedItem = item;
        await viewModel.RemoveItemAsync(item, TestContext.Current.CancellationToken);

        Assert.Null(viewModel.SelectedItem);
        Assert.False(viewModel.HasVisibleItems);
        Assert.Equal("当前分类还没有磁贴", viewModel.EmptyStateText);
    }

    private static async Task<TileLauncherViewModel> CreateLoadedViewModelAsync(
        ILauncherStateStore store,
        CancellationToken cancellationToken)
    {
        var viewModel = new TileLauncherViewModel(store, new FakeProcessLauncher());
        await viewModel.LoadAsync(cancellationToken);
        return viewModel;
    }

    private static TileLauncherState CreateDefaultState() => new()
    {
        Categories = [new TileCategory { Id = "all", Name = "全部" }]
    };

    private static TileItem CreateTile(string path) => new()
    {
        Id = path,
        Title = path,
        TargetPath = path
    };

    private sealed class FakeStateStore(TileLauncherState state) : ILauncherStateStore
    {
        public int SaveCount { get; private set; }
        public TileLauncherState State { get; private set; } = state;
        public Task<TileLauncherState> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(State);
        public Task SaveAsync(TileLauncherState state, CancellationToken cancellationToken)
        {
            SaveCount++;
            State = state;
            return Task.CompletedTask;
        }
    }

    private sealed class DeferredSaveStateStore(TileLauncherState state) : ILauncherStateStore
    {
        private TaskCompletionSource? _saveCompletion;

        public int SaveCount { get; private set; }

        public Task<TileLauncherState> LoadAsync(CancellationToken cancellationToken)
            => Task.FromResult(state);

        public Task SaveAsync(TileLauncherState state, CancellationToken cancellationToken)
        {
            SaveCount++;
            _saveCompletion = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            return _saveCompletion.Task;
        }

        public void CompleteSave() => _saveCompletion?.SetResult();
    }

    private sealed class FailingSaveStateStore(TileLauncherState state) : ILauncherStateStore
    {
        public Task<TileLauncherState> LoadAsync(CancellationToken cancellationToken)
            => Task.FromResult(state);

        public Task SaveAsync(TileLauncherState state, CancellationToken cancellationToken)
            => Task.FromException(new IOException("simulated save failure"));
    }

    private sealed class FakeProcessLauncher : IProcessLauncher
    {
        public Task<LaunchResult> LaunchAsync(LightUpUI.Models.LauncherItem item, CancellationToken cancellationToken)
            => Task.FromResult(LaunchResult.Success);
    }

    private sealed class FakePathRevealService(LaunchResult result) : IPathRevealService
    {
        public string? LastPath { get; private set; }

        public Task<LaunchResult> RevealAsync(string path, CancellationToken cancellationToken)
        {
            LastPath = path;
            return Task.FromResult(result);
        }
    }
}
