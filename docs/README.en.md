# LightUp

[简体中文](../README.md)

LightUp is a Windows desktop launcher built with .NET 10 and Avalonia. It combines a categorized tile workspace with a globally summoned search launcher: keep frequently used apps, folders, shortcuts, and links in a workspace, then launch them from tiles or search.

> LightUp is under active development. The core launcher workflow is usable today; the full-search, plugin-platform, and productization work is still evolving.

## Highlights

### Tile launcher

- A standalone, draggable launcher window. It is shown at startup and can also be opened from the tray or a global hotkey.
- Category navigation can be placed on the left or at the top. **All** is a virtual aggregate view; **Uncategorized** is a protected, persisted home for items.
- Create, rename, reorder, and remove categories. Removal can move items to a selected destination or delete them with the category. It requires confirmation and supports undo.
- Add files, folders, applications, `.lnk` files, `.url` files, and HTTP(S) links through the picker or drag and drop.
- Reorder tiles by drag and drop, move between categories, use keyboard ordering, edit names and notes, manage custom icons, and undo the most recent tile removal.
- Uses Windows Shell icons where available. Unavailable targets are marked and expose direct repair and removal actions.
- Compact and comfortable tile densities; window position, size, and topmost state are restored.

### Search launcher

- Global search window with keyboard navigation, opening, result context actions, and topmost support.
- **Simple mode** searches only the items saved in the tile launcher. Empty queries show commonly used items ordered by last launch and launch count.
- **Full mode** additionally searches common shortcuts, executables available on `PATH`, built-in actions, and enabled plugins.
- Built-in actions open the tile launcher, settings, and the `es:` Everything search protocol.
- Search results retain tile custom icons, usage information, and notes. Failed launches return actionable feedback.

### Settings and runtime behavior

- Separate global hotkeys: `Alt + Space` for search and `Alt + Shift + Space` for the tile launcher by default.
- Tray actions open search, the tile launcher, and settings, and can exit the application.
- System, light, and dark theme modes with preset and custom accent colors.
- Configurable result limit, tile search scope, category-navigation placement, tile density, hotkeys, and plugin enablement/weight.
- Configuration loading falls back safely for damaged or legacy files. Saves use a temporary-file replacement flow to avoid partial writes.

## Quick start

### Requirements

- Windows 10/11 x64
- .NET 10 SDK when building from source
- Everything is optional. LightUp uses it only through the full-mode `es:` action and never installs or silently starts it.

### Run from source

```powershell
git clone https://github.com/bbdd2729/LightUp.git
Set-Location LightUp
dotnet restore LightUp.slnx
dotnet run --project LightUpUI/LightUpUI.csproj
```

The tile workspace is the default startup surface. Drop files, folders, or shortcuts into it, or use the add button to choose entries.

### Build and test

```powershell
dotnet build LightUp.slnx -c Release
dotnet test LightUpTest/LightUpTest.csproj -c Release
```

GitHub Actions builds, tests, and uploads a self-contained Windows x64 package on every push. The manually dispatched **Release Windows Package** workflow accepts a semantic version, builds the package, creates a `v<version>` tag, and publishes a GitHub Release.

## Using the launcher

### Working with tiles

1. Create a category, or drop entries into the selected category.
2. Select a tile with one click; double-click it or press `Enter` to open it.
3. Press `F2` to rename the selected tile. Edit notes and choose a move destination from the bottom toolbar.
4. Drag a tile to another category to move it. Drop it on the upper or lower half of another tile to reorder it.
5. Use the context menu to reveal a target in Explorer, retarget unavailable entries, set or clear a custom icon, or remove the item.

When removing a category, choose whether to move its entries to a destination category or delete its entries. Both operations are immediately reversible from the bottom toolbar.

### Search modes

| Mode | Search scope | Best for |
| --- | --- | --- |
| Simple | Saved tile entries only | A focused personal workspace |
| Full | Tiles, shortcuts, PATH executables, built-in actions, and plugins | A broader desktop-launcher workflow |

The Everything result currently delegates to the locally installed Everything application through Windows `es:`. It is not yet a direct Everything IPC file-result provider.

## Local data

All user data stays on the local machine:

- `%LOCALAPPDATA%\LightUp\launcher.json`: categories, tiles, ordering, usage, notes, and custom icon paths.
- `%LOCALAPPDATA%\LightUp\search-launcher.json`: search mode, hotkeys, themes, window state, layout, and plugin settings.

Do not commit these files: they may contain personal paths and shortcut data. Deleting them resets LightUp to its defaults and clears personal launcher data.

## Plugins

LightUp currently provides a minimal in-process search-plugin entry point. Place a plugin DLL in the published `Plugins` folder. A plugin implements `ISearchProviderPlugin` and returns an `ISearchProvider`; it can then participate in full-mode search. Settings can enable or disable the plugin and adjust its weight.

The current plugin host is intentionally basic. It does not yet provide manifests, permissions, unloadable contexts, or an installation UI. Do not load untrusted DLLs.

## Repository layout

- `LightUpUI`: Avalonia UI, ViewModels, services, storage, and Windows integrations.
- `LightUpTest`: xUnit coverage for search, settings, hotkeys, window behavior, tiles, drag and drop, icons, and target health.
- `.github/workflows`: Windows build and manual release automation.

The implementation follows MVVM: ViewModels own state and commands, services own persistence/search/hotkey/window/platform behavior, and policy classes contain unit-testable rules.

## Roadmap

### P0 - Stability

- [x] Persistence, settings migration, window state, hotkeys, and tray foundations.
- [x] Tile categories, safe category removal with undo, drag-to-move, target repair, and launch-failure feedback.
- [ ] Repeatable Windows UI smoke coverage for category removal, window lifetime, and tray paths.

### P1 - Core experience

- [x] Simple/full search modes, tile sorting, theme settings, and the basic plugin entry point.
- [ ] Drag-in previews, rejected-item details, and native drag-and-drop verification.
- [ ] An asynchronous, cacheable, invalidatable tile icon provider and a fuller custom-icon workflow.

### P2 - Search and platform

- [ ] A unified search result/action contract.
- [ ] A real Everything file provider, query prefixes, calculator/URL/system actions, and search history.
- [ ] Single-instance coordination, startup-on-login, diagnostic logs, and a defined release/update strategy.

### P3 - Plugin platform

- [ ] A stable plugin platform with manifests, diagnostics, isolation boundaries, and a management UI.

## Contributing

Issues and improvements are welcome. Keep MVVM boundaries clear, add coverage for new behavior, and run:

```powershell
dotnet build LightUp.slnx -c Release
dotnet test LightUpTest/LightUpTest.csproj -c Release
```

## License

LightUp is distributed under the [GNU General Public License v3.0](../LICENSE).
