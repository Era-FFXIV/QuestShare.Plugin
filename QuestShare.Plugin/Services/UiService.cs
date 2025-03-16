using Dalamud.Interface.Windowing;
using QuestShare.Windows.MainWindow;

namespace QuestShare.Services
{
    internal class UiService : IService
    {
        public static WindowSystem WindowSystem = new("QuestShare");
        public static MainWindow MainWindow { get; private set; } = new();
        public static string LastErrorMessage { get; set; } = string.Empty;
        public static string LastServerMessage { get; set; } = string.Empty;

        public void Initialize()
        {
            WindowSystem.AddWindow(MainWindow);

            PluginInterface.UiBuilder.Draw += DrawUI;

            // This adds a button to the plugin installer entry of this plugin which allows
            // to toggle the display status of the configuration ui
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

            // Adds another button that is doing the same but for the main ui of the plugin
            PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
        }
        public void Shutdown()
        {
            WindowSystem.RemoveAllWindows();
            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;
            PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;
            MainWindow.Dispose();
        }

        private static void DrawUI() => WindowSystem.Draw();
        public static void ToggleConfigUI() => MainWindow.Toggle();
        public static void ToggleMainUI() => MainWindow.Toggle();
    }
}
