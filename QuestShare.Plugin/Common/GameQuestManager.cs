using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel;
using Lumina.Extensions;
using Newtonsoft.Json;
using QuestShare.Services;
using System.Numerics;


namespace QuestShare.Common
{
    internal static class GameQuestManager
    {
        public static List<GameQuest> GameQuests { get; private set; } = new List<GameQuest>();
        public static List<GameQuest> SharedCache { get; private set; } = new List<GameQuest>();
        private static byte LastStep = 0;
        private static uint LastQuestId = 0;
        public static void Initialize()
        {
            ClientState.Login += OnLogin;
            ClientState.Logout += OnLogout;
            if (GameQuests.Count == 0)
            {
                Task.Run(LoadQuests);
            }
            Framework.Update += OnFrameworkUpdate;
        }
        public static void Dispose()
        {
            ClientState.Login -= OnLogin;
            ClientState.Logout -= OnLogout;
            Framework.Update -= OnFrameworkUpdate;
        }

        public static void LoadQuests()
        {
            var activeQuest = GetActiveQuest();
            GameQuests.Clear();
            // iterate through all quests in the game to see which ones are currently active
            foreach (var quest in SheetManager.QuestSheet)
            {
                if (quest.Id == "" || quest.RowId == 0)
                {
                    continue;
                }
                if (QuestManager.GetQuestSequence(quest.RowId) > 0)
                {
                    Log.Debug($"Adding quest {quest.RowId} to GameQuests - {quest.Name} - Progression {QuestManager.GetQuestSequence(quest.RowId)-1}");
                    GameQuests.Add(new GameQuest(quest.RowId));
                }
            }
            if (activeQuest != null && !GameQuests.Any(q => q.QuestId == activeQuest.QuestId))
            {
                Log.Debug($"Active quest {activeQuest.QuestId} was not found in GameQuests, assuming completed.");
                HostService.Update(0, 0);
                return;
            }
            else if (activeQuest != null)
            {
                SetActiveFlag(activeQuest.QuestId);
            }
            else if (ConfigurationManager.Instance.OwnedSession != null && ConfigurationManager.Instance.OwnedSession.ActiveQuestId != 0)
            {
                var quest = GetQuestById((uint)ConfigurationManager.Instance.OwnedSession.ActiveQuestId);
                SetActiveFlag(quest.QuestId);
            }
        }

        private static void OnLogin()
        {
            LoadQuests();
        }

        private static void OnLogout(int code, int _)
        {
            GameQuests.Clear();
            SharedCache.Clear();
        }

        public unsafe static bool TrackMsq()
        {
            var api = (ApiService)Plugin.GetService<ApiService>();
            var share = (ShareService)Plugin.GetService<ShareService>();
            if (ConfigurationManager.Instance.AutoShareMsq)
            {
                var questId = (uint)AgentScenarioTree.Instance()->Data->CurrentScenarioQuest;
                if (questId == 0) return false;
                else questId += 0x10000U;
                if (GetActiveQuest() != null && GetActiveQuest()?.QuestId == questId) return false;
                if (SheetManager.QuestSheet.TryGetRow(questId, out var row))
                {
                    if (row.RowId == 0) return false;
                    var quest = GetQuestById(questId);
                    if (GameQuests.Contains(quest) && GetActiveQuest()?.QuestId != questId)
                    {
                        SetActiveFlag(questId);
                        HostService.Update((int)questId, quest.CurrentStep);
                    }
                    else
                    {
                        GameQuests.Add(quest);
                        SetActiveFlag(questId);
                        HostService.Update((int)questId, quest.CurrentStep);
                    }
                    return true;
                }
                else
                {
                    Log.Debug($"Quest {questId} not found in QuestSheet");
                }
            }
            return false;
        }

        private static void OnFrameworkUpdate(IFramework framework)
        {
            if (PlayerState.ContentId == 0) return;
            TrackMsq();
            var q = GetActiveQuest();
            var api = (ApiService)Plugin.GetService<ApiService>();
            if (q != null && q.QuestId == LastQuestId)
            {
                var step = QuestManager.GetQuestSequence(q.QuestId);
                if (step != LastStep)
                {
                    if (LastStep == 0xFF && step == 0)
                    {
                        // quest was completed
                        Log.Debug($"Quest {q.QuestId} was completed");
                        // check for next quest in the chain
                        if (q.QuestData.PreviousQuest.TryGetFirst(out var prevQuest))
                        {
                            if (prevQuest.RowId == 0 || !prevQuest.IsValid)
                            {
                                Log.Debug("No previous quest in chain");
                                GameQuests.Remove(q);
                                LoadQuests();
                                return;
                            }
                            Log.Debug($"Previous quest in chain was {prevQuest.RowId}");
                            var nextQuest = SheetManager.QuestSheet.FirstOrDefault(qu => prevQuest.Value.RowId == q.QuestId);
                            if (nextQuest.RowId != 0)
                            {
                                Log.Debug($"Next quest in chain is {nextQuest.RowId}");
                                GameQuests.Add(new GameQuest(nextQuest.RowId));
                                SetActiveFlag(nextQuest.RowId);
                            }
                            else
                            {
                                Log.Debug("No next quest in chain");
                                GameQuests.Remove(q);
                                LoadQuests();
                                HostService.Update(0, 0);
                                return;
                            }
                        }
                        HostService.Update((int)q.QuestId, q.CurrentStep);
                    }
                    else
                    {
                        LastStep = step;
                        Log.Debug($"Quest step changed to {step}");
                        HostService.Update((int)q.QuestId, q.CurrentStep);
                    }
                }
            }
            else if (q != null)
            {
                LastQuestId = q.QuestId;
                LastStep = QuestManager.GetQuestSequence(q.QuestId);
            }
        }

        public static GameQuest? GetActiveQuest()
        {
            return GameQuests.FirstOrDefault(q => q.IsActive);
        }

        public static GameQuest GetQuestById(uint questId)
        {
            if (SharedCache.FirstOrDefault(q => q.QuestId == questId) is GameQuest quest)
            {
                return quest;
            } else
            {
                var newQuest = new GameQuest(questId);
                SharedCache.Add(newQuest);
                return newQuest;
            }
        }

        public static void SetActiveFlag(uint questId)
        {
            Log.Debug($"Setting active quest to {questId}");
            var quest = GameQuests.FirstOrDefault(q => q.QuestId == questId);
            if (quest != null)
            {
                quest.IsActive = true;
                // set all other quests to inactive
                foreach (var q in GameQuests)
                {
                    if (q.QuestId != questId)
                    {
                        q.IsActive = false;
                    }
                }
            }
        }
    }

    internal class GameQuest
    {
        public uint QuestId { get; init; }
        [JsonIgnore]
        public Lumina.Excel.Sheets.Quest QuestData { get; init; }
        public string QuestName => QuestData.Name.ExtractText();
        public byte CurrentStep => QuestManager.GetQuestSequence(QuestId);
        public List<string> QuestSteps { get; private set; } = [];
        public bool IsMainQuest => false; // TODO: Implement this
        public bool IsActive { get; set; } = false;
        public GameQuest(uint questId)
        {
            QuestId = questId;
            if (SheetManager.QuestSheet.TryGetRow(questId, out var quest))
            {
                QuestData = quest;
            }
            var sheet = TextSheetForQuest(QuestData);
            for(uint i = 0; i < sheet.Count; i++)
            {
                var row = sheet.GetRow(i);
                if (row.Key.ExtractText().Contains("_TODO_") && row.Value.ExtractText() != "")
                {
                    QuestSteps.Add(row.Value.ExtractText());
                }
            }
        }
        public Lumina.Excel.Sheets.Map GetMapLocation()
        {
            return QuestData.TodoParams.FirstOrDefault(param => param.ToDoCompleteSeq == CurrentStep)
                .ToDoLocation.FirstOrDefault(location => location is not { RowId: 0 }).Value.Map.Value;
        }

        public MapLinkPayload GetMapLink(byte step)
        {
            var toDoData = QuestData.TodoParams.Select(t => t.ToDoLocation).ToList();
            Log.Debug($"Step: {step}/{0xFF} - Total steps: {QuestSteps.Count} - ToDoParams Count: {toDoData.Count}");
            var data = toDoData.ElementAt(step > QuestSteps.Count ? QuestSteps.Count-1 : step).FirstOrDefault();
            if (!data.IsValid)
            {
                Log.Error("Invalid ToDoLocation data");
                return new MapLinkPayload(0, 0, 0, 0);
            }
            var coords = MapUtil.WorldToMap(new Vector3(data.Value.X, data.Value.Y, data.Value.Z), data.Value.Map.Value.OffsetX, data.Value.Map.Value.OffsetY, 0, data.Value.Map.Value.SizeFactor);
            var mapLink = new MapLinkPayload(data.Value.Territory.Value.RowId, data.Value.Map.Value.RowId, coords.X, coords.Y);
            return mapLink;
        }

        internal static ExcelSheet<QuestDialogue> TextSheetForQuest(Lumina.Excel.Sheets.Quest q)
        {
            var qid = q.Id.ToString();
            Log.Debug($"Getting text sheet for quest {q.Id} - RowID: {q.RowId}");
            var dir = qid.Substring(qid.Length - 5, 3);
            return DataManager.GetExcelSheet<QuestDialogue>(name: $"quest/{dir}/{qid}");
        }
    }
}
