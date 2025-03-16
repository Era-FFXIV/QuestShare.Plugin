using Dalamud.Game.Command;

namespace QuestShare.Services
{
    internal class CommandService : IService
    {
        public void Initialize()
        {
            CommandManager.AddHandler("/questshare", new CommandInfo(OnCommand)
            {
                HelpMessage = "Open the Quest Share window."
            });
        }
        public void Shutdown()
        {
            CommandManager.RemoveHandler("/questshare");
        }
        private static void OnCommand(string command, string args)
        {
            Log.Information($"Command received: {command} {args}");
            if (command == "/questshare")
            {
                UiService.ToggleMainUI();
            }
        }
    }
}
