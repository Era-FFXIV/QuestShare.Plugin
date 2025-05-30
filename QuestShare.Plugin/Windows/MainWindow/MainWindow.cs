using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Microsoft.AspNetCore.SignalR.Client;
using QuestShare.Services;
using static QuestShare.Common.ConfigurationManager;
using static QuestShare.Services.ApiService;
using QuestShare.Windows.MiniWindow;

namespace QuestShare.Windows.MainWindow;

public class MainWindow : Window, IDisposable
{
    public MainWindow()
        : base(Plugin.Name + "###Main", ImGuiWindowFlags.None)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(550, 500),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void OnOpen()
    {
    }

    public void Dispose() { }

    private ApiService ApiService => (ApiService)Plugin.GetService<ApiService>();
    private ShareService ShareService => (ShareService)Plugin.GetService<ShareService>();
    private PartyService PartyService => (PartyService)Plugin.GetService<PartyService>();
    private HostService HostService => (HostService)Plugin.GetService<HostService>();
    private GameQuest? selectedQuest = GameQuestManager.GetActiveQuest();

    private enum ActiveTab
    {
        Host,
        Join,
        Settings
    }
    public override void Draw()
    {
        ImGui.TextUnformatted("Server Status: "); ImGui.SameLine();
        DrawConnectionState();
        ImGui.SameLine();
        ImGui.BeginDisabled(ApiService.IsLockedOut);
        if (ApiService.IsConnected)
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Unlink, ImGuiColors.DPSRed))
            {
                _ = ApiService.Disconnect();
            }
        }
        else
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Link, ImGuiColors.DPSRed))
            {
                UiService.LastErrorMessage = "";
                UiService.LastServerMessage = "";
                _ = ApiService.Connect();
            }
        }
        ImGui.EndDisabled();
        ImGui.SameLine();
        // draw on right side of window
        ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 50);
        DrawSupportButton();
        ImGui.Separator();
        using (ImRaii.TabBar("MainTabBar", ImGuiTabBarFlags.NoCloseWithMiddleMouseButton))
        {
            ImGui.BeginDisabled(!ApiService.IsConnected);
            if (ImGui.BeginTabItem("Host Group"))
            {
                DrawHostTab();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Join Group"))
            {
                DrawJoinTab();
                ImGui.EndTabItem();
            }
            ImGui.EndDisabled();
            if (ImGui.BeginTabItem("Settings"))
            {
                DrawSettingsTab();
                ImGui.EndTabItem();
            }
        }
        ImGui.Separator();
        if (UiService.LastErrorMessage != "")
        {
            ImGui.TextColored(ImGuiColors.DPSRed, UiService.LastErrorMessage);
        }
    }

    private void DrawConnectionState()
    {
        if (UiService.LastServerMessage != "")
        {
            ImGui.TextColored(ImGuiColors.DalamudYellow, UiService.LastServerMessage);
            return;
        }
        switch (this.ApiService.ConnectionState)
        {
            case HubConnectionState.Connecting:
                ImGui.TextColored(ImGuiColors.DalamudYellow, "Connecting...");
                break;
            case HubConnectionState.Connected:
                ImGui.TextColored(ImGuiColors.HealerGreen, "Connected");
                break;
            case HubConnectionState.Disconnected:
                ImGui.TextColored(ImGuiColors.DPSRed, "Disconnected");
                break;
            case HubConnectionState.Reconnecting:
                ImGui.TextColored(ImGuiColors.DalamudYellow, "Reconnecting...");
                break;
            default:
                break;
        }
    }

    private bool generatePending = false;

    private void DrawHostTab()
    {
        if (HostService.ActiveSession != null && HostService.ActiveSession.ShareCode != null) generatePending = false;
        ImGui.TextUnformatted("Share Code:");
        ImGui.SameLine();
        if (HostService.ActiveSession != null)
        {
            ImGui.TextColored(ImGuiColors.HealerGreen, HostService.ActiveSession.ShareCode);
            if (ImGui.IsItemClicked())
            {
                ImGui.SetClipboardText(HostService.ActiveSession.ShareCode);
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Click to copy to clipboard.");
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                DispatchCancel();
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Cancel the current session. This will permanently remove the share code and all connected clients.");
            }
            var allowJoins = HostService.AllowJoins;
            var skipPartyCheck = HostService.SkipPartyCheck;
            var sendUpdates = HostService.IsActive;
            if (ToggleButtonWithHelpMarker("Allow new Joins", "Allows new players to join the group.", ref allowJoins))
            {
                HostService.SetAllowJoins(allowJoins);
            }
            if (ToggleButtonWithHelpMarker("Skip Party Check", "Allows new players to join the group without being in your party first.", ref skipPartyCheck))
            {
                HostService.SetSkipPartyCheck(skipPartyCheck);
            }
            if (ToggleButtonWithHelpMarker("Send Updates", "Sends quest updates to the server.", ref sendUpdates))
            {
                HostService.SetIsActive(sendUpdates);
            }
            var autoShareMsq = Instance.AutoShareMsq;
            var autoShareNewQuests = Instance.AutoShareNewQuests;
            ImGui.BeginDisabled(autoShareNewQuests);
            if (ToggleButtonWithHelpMarker("Auto Share MSQ", "Automatically share the Main Scenario Quest with your group when it is accepted.", ref autoShareMsq))
            {
                Instance.AutoShareMsq = autoShareMsq;
                if (Instance.AutoShareNewQuests)
                {
                    Instance.AutoShareNewQuests = false;
                }
                Save();
            }
            ImGui.EndDisabled();
            
            ImGui.BeginDisabled(autoShareMsq);
            if (ToggleButtonWithHelpMarker("Auto Share New Quests", "Automatically share new quests with your group when they are accepted. Mutually exclusive with MSQ share.", ref autoShareNewQuests))
            {
                Instance.AutoShareNewQuests = autoShareNewQuests;
                if (Instance.AutoShareMsq)
                {
                    Instance.AutoShareMsq = false;
                }
                Save();
            }
            ImGui.EndDisabled();
            ImGui.BeginDisabled(autoShareMsq);
            using (var combo = ImRaii.Combo("##Quests", GameQuestManager.GetActiveQuest()?.QuestName ?? "---SELECT---", ImGuiComboFlags.HeightRegular))
            {
                if (combo)
                {
                    foreach (var quest in GameQuestManager.GameQuests.OrderBy(q => q.QuestName))
                    {
                        if (ImGui.Selectable(quest.QuestName))
                        {
                            selectedQuest = quest;
                            GameQuestManager.SetActiveFlag(quest.QuestId);
                            HostService.Update((int)quest.QuestId, quest.CurrentStep);
                            Save();
                        }
                    }
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("Refresh"))
            {
                GameQuestManager.LoadQuests();
            }
            ImGui.EndDisabled();

            // add a line of space
            ImGui.TextUnformatted(" ");
            if (selectedQuest == null && GameQuestManager.GetActiveQuest() == null)
            {
                ImGui.TextUnformatted("No quest selected.");
                return;
            }
            else if (GameQuestManager.GetActiveQuest() != null)
            {
                selectedQuest = GameQuestManager.GetActiveQuest();
            }
            ImGui.TextUnformatted("Active Quest:");
            ImGui.SameLine();
            ImGui.TextUnformatted(selectedQuest!.QuestName);
            ImGui.Separator();

            var steps = selectedQuest.QuestSteps;
            for (var i = 0; i < steps!.Count; i++)
            {
                var questText = Instance.HideFutureStepsHost && i + 1 > selectedQuest.CurrentStep ? "???" : steps[i];
                if (i + 1 == selectedQuest.CurrentStep || (selectedQuest.CurrentStep == 0 && i == 0) || (selectedQuest.CurrentStep == 0xFF && i + 1 == steps.Count))
                {
                    ImGui.TextColored(ImGuiColors.HealerGreen, questText);
                }
                else if (i + 1 < selectedQuest.CurrentStep)
                {
                    ImGui.TextColored(ImGuiColors.DalamudYellow, questText);
                }
                else
                {
                    ImGui.TextUnformatted(questText);
                }
            }

        }
        else
        {
            ImGui.BeginDisabled(generatePending);
            if (ImGui.Button("Generate New"))
            {
                DispatchRegister();
            }
            ImGui.EndDisabled();
        }

    }

    private string enteredShareCode = "";
    private bool isJoining = false;
    private void OnGroupJoin(object? sender, GroupJoinEventArgs args)
    {
        isJoining = false;
        ApiService.GroupJoined -= OnGroupJoin;
    }
    private void DrawJoinTab()
    {
        ImGui.TextUnformatted("Enter Share Code:");
        ImGui.SameLine();
        ImGui.BeginDisabled(isJoining);
        ImGui.InputText("##ShareCode", ref enteredShareCode, 8);
        ImGui.SameLine();
        var btn = "Join";
        if (isJoining) btn = "Joining...";
        if (ImGui.Button(btn))
        {
            var payload = new Objects.ShareCode { CharacterId = ClientState.LocalContentId.ToString().SaltedHash(enteredShareCode), Code = enteredShareCode };
            isJoining = true;
            ApiService.GroupJoined += OnGroupJoin;
            DispatchGroup(payload);
        }
        ImGui.EndDisabled();
        ImGui.Separator();
        ImGui.TextUnformatted("Currently Joined Groups");
        if (ShareService.Sessions.Count == 0)
        {
            ImGui.TextUnformatted("No groups joined.");
        }
        else
        {
            foreach (var session in ShareService.Sessions)
            {
                using var tree = ImRaii.TreeNode($"Session: {session.ShareCode}");
                if (tree)
                {
                    DrawSessionDetails(session);
                    ImGui.SameLine();
                    if (ImGui.Button("Show Mini"))
                    {
                        UiService.MiniWindow.SetSession(session.ShareCode);
                        UiService.MiniWindow.Toggle();
                    }
                    if (ImGui.Button("Leave Group"))
                    {
                        DispatchUngroup(session);
                    }
                }

            }
        }
    }

    public void DrawSessionDetails(Objects.Session session, bool isMini = false)
    {
        ImGui.TextUnformatted($"Owner: {ShareService.GetShareCodeOwner(session.ShareCode)}");
        var activeQuest = session.ActiveQuestId;
        var activeStep = session.ActiveQuestStep;
        if (activeQuest != 0)
        {
            var questInfo = GameQuestManager.GetQuestById((uint)activeQuest);
            var steps = questInfo.QuestSteps;

            ImGui.TextUnformatted(questInfo.QuestData.Name.ExtractText());
            ImGui.Separator();
            for (var i = 0; i < steps.Count; i++)
            {
                var questText = Instance.HideFutureStepsMember && i + 1 > activeStep ? "???" : steps[i];
                if (i + 1 == activeStep || (i + 1 == steps.Count && activeStep == 0xFF))
                {
                    ImGui.TextColored(ImGuiColors.HealerGreen, questText);
                }
                else
                {
                    if (!Instance.HideStepsInMiniWindow || !isMini)
                    {
                        ImGui.TextUnformatted(questText);
                    }
                }
            }
            if (ImGui.Button("Get Marker"))
            {
                var marker = questInfo.GetMapLink((byte)(activeStep - 1));
                if (marker != null)
                    GameGui.OpenMapWithMapLink(marker);
                else
                    Log.Error("No map link available for this quest.");

            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Generates a map marker for the current step's destination.");
            }
            
        }
        else
        {
            ImGui.TextUnformatted("No active quest.");
        }

    }

    string newServerDisplayName = "";
    string newServerUrl = "";

    private void DrawSettingsTab()
    {
        var selectedApiServer = Instance.ApiDisplayName;
        ImGui.BeginDisabled(Instance.ApiConfigurations.Count < 1);
        if (ImGui.BeginCombo("API Server", selectedApiServer))
        {
            foreach (var server in Instance.ApiConfigurations)
            {
                var isSelected = selectedApiServer == server.DisplayName;
                if (ImGui.Selectable(server.DisplayName, isSelected))
                {
                    var index = Array.FindIndex<ApiConfiguration>([.. Instance.ApiConfigurations], x => x.DisplayName == server.DisplayName);
                    Instance.ApiConfigurations[index].Active = true;
                    foreach (var config in Instance.ApiConfigurations)
                    {
                        if (config.DisplayName != server.DisplayName)
                        {
                            config.Active = false;
                        }
                    }
                    Save();
                    Framework.Run(ApiService.OnUrlChange);
                }
                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }
            ImGui.EndCombo();
        }
        ImGui.EndDisabled();
        ImGui.SameLine();
        if (ImGui.Button("Add"))
        {
            // pop up a dialog to add a new server
            ImGui.OpenPopup("Add Server");
        }
        ImGui.SameLine();
        ImGui.BeginDisabled(Instance.ApiConfigurations.Count < 1);
        if (ImGui.Button("Delete"))
        {
            // pop up a dialog to delete the selected server
            ImGui.OpenPopup("Delete?");
        }
        ImGui.EndDisabled();
        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        var open = true;
        using (var addServer = ImRaii.PopupModal("Add Server", ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (addServer)
            {
                ImGui.TextUnformatted("Add a new API server configuration.");
                ImGui.InputText("Display Name", ref newServerDisplayName, 64);
                ImGui.InputText("URL", ref newServerUrl, 200);
                ImGui.TextUnformatted("Note: The URL should be the full URL to the hub endpoint and MUST be http:// or https:// (preferred)");
                bool isValid = Uri.TryCreate(newServerUrl, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                ImGui.BeginDisabled(!isValid || string.IsNullOrEmpty(newServerDisplayName) || string.IsNullOrEmpty(newServerUrl));
                if (ImGui.Button("Save"))
                {
                    if (Instance.ApiConfigurations.Count == 0)
                    {
                        // add default server first
                        Instance.ApiConfigurations.Add(new ApiConfiguration { DisplayName = ConfigurationManager.Instance.ApiDisplayName, Url = ConfigurationManager.Instance.ApiUrl, Active = false });
                    }
                    Instance.ApiConfigurations.Add(new ApiConfiguration { DisplayName = newServerDisplayName, Url = newServerUrl, Active = true });
                    Save();
                    ImGui.CloseCurrentPopup();
                    newServerUrl = "";
                    newServerDisplayName = "";
                }
                ImGui.EndDisabled();
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                    newServerUrl = "";
                    newServerDisplayName = "";
                }
            }
        }
        var deleteServer = false;
        using (var delServer = ImRaii.PopupModal("Delete?", ref deleteServer, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (delServer)
            {
                ImGui.TextUnformatted("Are you sure you want to delete this server?");
                if (ImGui.Button("Yes"))
                {
                    var index = Array.FindIndex<ApiConfiguration>([.. Instance.ApiConfigurations], x => x.DisplayName == selectedApiServer);
                    Instance.ApiConfigurations.RemoveAt(index);
                    Save();
                    ImGui.CloseCurrentPopup();
                    Framework.Run(ApiService.OnUrlChange);
                }
                ImGui.SameLine();
                if (ImGui.Button("No"))
                {
                    ImGui.CloseCurrentPopup();
                }
            }
        }
        var connectOnStartup = Instance.ConnectOnStartup;
        if (ToggleButtonWithHelpMarker("Connect on Startup", "Automatically connect to the selected API server when the game is started.", ref connectOnStartup))
        {
            Instance.ConnectOnStartup = connectOnStartup;
            Save();
        }
        var hideFutureStepsHost = Instance.HideFutureStepsHost;
        if (ToggleButtonWithHelpMarker("Hide Future Steps (Host)", "Hides future steps of the quest from the UI when viewing your hosted quest. This does not affect the quest sharing process.", ref hideFutureStepsHost))
        {
            Instance.HideFutureStepsHost = hideFutureStepsHost;
            Save();
        }
        var hideFutureStepsMember = Instance.HideFutureStepsMember;
        if (ToggleButtonWithHelpMarker("Hide Future Steps (Member)", "Hides future steps of the quest from the UI when viewing a shared quest. This does not affect the quest sharing process.", ref hideFutureStepsMember))
        {
            Instance.HideFutureStepsMember = hideFutureStepsMember;
            Save();
        }
        var hideStepsInMiniWindow = Instance.HideStepsInMiniWindow;
        if (ToggleButtonWithHelpMarker("Hide Steps in Mini Window", "Hides the steps of the quest from the mini window, only showing the current step.", ref hideStepsInMiniWindow))
        {
            Instance.HideStepsInMiniWindow = hideStepsInMiniWindow;
            Save();
        }
    }

    private static bool ToggleButtonWithHelpMarker(string label, string helpText, ref bool v)
    {
        ImGui.TextUnformatted(label);
        ImGui.SameLine();
        var result = ImGuiComponents.ToggleButton(label, ref v);
        ImGuiComponents.HelpMarker(helpText);
        return result;
    }

    private static void DrawSupportButton()
    {
        if (ImGuiComponents.IconButton(FontAwesomeIcon.QuestionCircle))
        {
            ImGui.OpenPopup("Support");
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Need help? Click here to get support.");
        }
        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        var open = true;
        using (var support = ImRaii.PopupModal("Support", ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (support)
            {
                ImGui.TextUnformatted("If you need help, please visit the Dalamud Discord server and ask in the appropriate channel under plugin-help-forum.");
                ImGui.TextUnformatted("Discord: ");
                ImGui.SameLine();
                ImGui.TextUnformatted("https://discord.gg/AdQWDjUQRA");
                if (ImGui.IsItemClicked())
                {
                    OpenUrl("https://discord.gg/AdQWDjUQRA");
                }
                ImGui.TextUnformatted("GitHub: ");
                ImGui.SameLine();
                ImGui.TextUnformatted("https://github.com/Era-FFXIV/QuestShare.Plugin");
                if (ImGui.IsItemClicked())
                {
                    OpenUrl("https://github.com/Era-FFXIV/QuestShare.Plugin");
                }
                if (ImGui.Button("Close"))
                {
                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }
    private static void OpenUrl(string url)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
    }
}