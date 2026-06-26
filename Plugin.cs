using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using SimpleRollTracker.Windows;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;

namespace SimpleRollTracker;

/// <summary>
/// Main entry point for the SimpleRollTracker plugin.
/// Handles initialization of Dalamud services, UI configuration, and disposal.
/// </summary>
public sealed class Plugin : IDalamudPlugin {
    public string Name => "SimpleRollTracker";

    public const string CommandName = "/rolltracker";

    public Configuration Configuration;
    private readonly GameManager gameManager;

    public readonly WindowSystem WindowSystem = new("SimpleRollTracker");
    public MainWindow MainWindow;

    public Plugin(IDalamudPluginInterface pluginInterface) {
        pluginInterface.Create<Services>();
        this.Configuration = Services.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this.gameManager = new GameManager(this.Configuration);
        this.MainWindow = new MainWindow(this.gameManager, this.Configuration);
        this.WindowSystem.AddWindow(this.MainWindow);
        Services.PluginInterface.UiBuilder.Draw += DrawUI;
        Services.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        Services.PluginInterface.UiBuilder.OpenMainUi += DrawMainUI;
        Services.CommandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand) { HelpMessage = "Open the SimpleRollTracker window" });
    }

    public void Dispose() {
        this.WindowSystem.RemoveAllWindows();
        this.MainWindow.Dispose();
        this.gameManager.Dispose();
        Services.CommandManager.RemoveHandler(CommandName);
        Services.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        Services.PluginInterface.UiBuilder.OpenMainUi -= DrawMainUI;
        Services.PluginInterface.UiBuilder.Draw -= DrawUI;
    }

    private void OnCommand(string command, string args) {
        this.MainWindow.IsOpen = true;
    }

    private void DrawUI() {
        this.WindowSystem.Draw();
    }

    private void DrawConfigUI() {
        this.MainWindow.IsOpen = true;
    }

    private void DrawMainUI() {
        this.MainWindow.IsOpen = true;
    }
}
