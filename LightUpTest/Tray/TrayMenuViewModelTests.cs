using LightUpUI.ViewModels;

namespace LightUpTest.Tray;

public sealed class TrayMenuViewModelTests
{
    [Fact]
    public void Quick_menu_commands_dispatch_to_the_matching_application_action()
    {
        var calls = new List<string>();
        var viewModel = new TrayMenuViewModel(
            () => calls.Add("search"),
            () => calls.Add("tiles"),
            () => calls.Add("settings"),
            () => calls.Add("exit"));

        viewModel.OpenSearchCommand.Execute(null);
        viewModel.OpenTilesCommand.Execute(null);
        viewModel.OpenSettingsCommand.Execute(null);
        viewModel.ExitApplicationCommand.Execute(null);

        Assert.Equal(["search", "tiles", "settings", "exit"], calls);
    }
}
