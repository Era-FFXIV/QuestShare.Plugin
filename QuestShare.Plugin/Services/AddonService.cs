using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace QuestShare.Services
{
    internal class AddonService : IService
    {
        public void Initialize()
        {
            AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, ["JournalAccept"], JournalAccept_OnPostReceiveEvent);
        }
        public void Shutdown()
        {
            AddonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, ["JournalAccept"], JournalAccept_OnPostReceiveEvent);
        }

        private unsafe void JournalAccept_OnPostReceiveEvent(AddonEvent e, AddonArgs args)
        {
            if (args is AddonReceiveEventArgs j)
            {                
                var gameAddon = GameGui.GetAddonByName("JournalAccept");
                if (gameAddon == null) return;
                var addon = (AtkUnitBase*)gameAddon.Address;
                if (addon == null) return;
                var questId = addon->AtkValues[261].UInt;
                if (questId == 0)
                {
                    Log.Warning("Failed to get quest ID, maybe the values changed?");
                    return;
                }
                Log.Debug($"Quest: {questId}");
                questId = questId + 0x10000U;
                Log.Debug($"Translated quest: {questId}");
                if (j.EventParam == 1)
                {
                    Log.Debug($"Accepting quest: {questId}");
                    if (ConfigurationManager.Instance.AutoShareNewQuests)
                    {
                        if (SheetManager.QuestSheet.TryGetRow(questId, out var quest))
                        {
                            Log.Info($"Auto sharing quest: {questId} {quest.Name}");
                            GameQuestManager.GameQuests.Add(new GameQuest(questId));
                            GameQuestManager.SetActiveFlag(questId);
                        }
                        else
                        {
                            Log.Error($"Failed to find quest: {questId}");
                        }
                    }
                }
                else if (j.EventParam == 2)
                {
                    Log.Debug($"Declining quest: {questId}");
                }
                else
                {
                    Log.Debug($"Unknown event param: {j.EventParam}");
                }
            }
        }
    }
}
