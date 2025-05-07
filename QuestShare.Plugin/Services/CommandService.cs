using Dalamud.Game.Command;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace QuestShare.Services
{
    internal class CommandService : IService
    {
        public void Initialize()
        {
            CommandManager.AddHandler("/questshare", new CommandInfo(MainUi)
            {
                HelpMessage = "Open the Quest Share window."
            });
            CommandManager.AddHandler("/questsharemini", new CommandInfo(MiniUi)
            {
                HelpMessage = "Open the Quest Share mini window. Requires a valid share code entered."
            });
            CommandManager.AddHandler("/qsmini", new CommandInfo(MiniUi)
            {
                HelpMessage = "Open the Quest Share mini window. Requires a valid share code entered."
            });
        }
        public void Shutdown()
        {
            CommandManager.RemoveHandler("/questshare");
            CommandManager.RemoveHandler("/questsharemini");
            CommandManager.RemoveHandler("/qsmini");
        }
        private static void MainUi(string command, string args)
        {
            Log.Information($"Command received: {command} {args}");
            if (command == "/questshare")
            {
                UiService.ToggleMainUI();
            }
        }

        private static void MiniUi(string command, string args)
        {
            if (args == "" && UiService.MiniWindow.ShareCode != "")
            {
                UiService.MiniWindow.Toggle();
            }
            else if (args == "" && ShareService.ShareCodes.Count == 1)
            {
                UiService.MiniWindow.SetSession(ShareService.ShareCodes.First().Code);
                UiService.MiniWindow.Toggle();
            }
            else if (ShareService.ShareCodes.FirstOrDefault(s => s.Code == args) != null)
            {
                UiService.MiniWindow.SetSession(args);
                UiService.MiniWindow.Toggle();
            }
            else
            {
                ChatGui.PrintError($"No share code found for {args} or one was not provided.");
            }
        }
    }
}
