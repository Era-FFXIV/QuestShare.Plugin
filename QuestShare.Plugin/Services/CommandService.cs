using Dalamud.Game.Command;
using Dalamud.Game.Text;

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
            CommandManager.AddHandler("/qsnext", new CommandInfo(NextQuestObjective)
            {
                HelpMessage = "Print the next quest objective and a map marker. Optionally supply \"flag\" or \"step\" for just the flag or step text."
            });
        }
        public void Shutdown()
        {
            CommandManager.RemoveHandler("/questshare");
            CommandManager.RemoveHandler("/questsharemini");
            CommandManager.RemoveHandler("/qsmini");
            CommandManager.RemoveHandler("/qsnext");
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

        private static void NextQuestObjective(string command, string args)
        {
            var activeSession = UiService.MiniWindow?.GetSession();
            if (activeSession == null)
            {
                ChatGui.Print(new XivChatEntry { Type = XivChatType.Echo, Message = "No active session. Open the mini window first." });
                return;
            }
            byte step = (byte)(activeSession.ActiveQuestStep - 1);
            var questInfo = GameQuestManager.GetQuestById((uint)activeSession.ActiveQuestId);
            if (args.Length > 0)
            {
                if (args == "flag")
                {
                    ChatGui.Print(new XivChatEntry { Type = XivChatType.Echo, Message = questInfo.GetFullMapLink(step) });
                }
                else if (args == "step")
                {
                    ChatGui.Print(new XivChatEntry { Type = XivChatType.Echo, Message = questInfo.QuestSteps[step] ?? "No active quest." });
                }
                else
                {
                    ChatGui.PrintError($"Invalid argument {args}. Use \"flag\" or \"step\".");
                }
            }
            else
            {
                ChatGui.Print(new XivChatEntry { Type = XivChatType.Echo, Message = $"Next Step: {questInfo.QuestSteps[step] ?? "No active quest."}" });
                ChatGui.Print(new XivChatEntry { Type = XivChatType.Echo, Message = questInfo.GetFullMapLink(step) });
            }
        }
    }
}
