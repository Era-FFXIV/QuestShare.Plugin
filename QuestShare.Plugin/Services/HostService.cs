using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestShare.Services
{
    internal class HostService : IService
    {
        internal static Objects.OwnedSession? Session => ConfigurationManager.Instance.OwnedSession;
        internal static bool HostEnabled => ConfigurationManager.Instance.EnableHosting;
        internal static Objects.Session? ActiveSession => Session?.Session;
        internal static int ActiveQuestId => ActiveSession?.ActiveQuestId ?? 0;
        internal static byte ActiveQuestStep => ActiveSession?.ActiveQuestStep ?? 0;
        internal static bool IsHost => ActiveSession != null;
        internal static bool IsActive => Session?.IsActive ?? false;
        internal static bool AllowJoins => Session?.AllowJoins ?? true;
        internal static bool SkipPartyCheck => Session?.SkipPartyCheck ?? false;
        private ulong CharacterId { get; set; }

        public void Initialize()
        {
            ClientState.Login += OnLogin;
            ClientState.Logout += OnLogout;
            CharacterId = ClientState.LocalContentId;
        }

        public void Shutdown()
        {
            ClientState.Login -= OnLogin;
            ClientState.Logout -= OnLogout;
        }

        private void OnLogin()
        {
            CharacterId = ClientState.LocalContentId;
        }

        private void OnLogout(int _, int __)
        {
            CharacterId = 0;
        }

        public void Start(string shareCode)
        {
            var session = new Objects.Session { OwnerCharacterId = CharacterId.ToString().SaltedHash(shareCode), ShareCode = shareCode, ActiveQuestId = ActiveQuestId, ActiveQuestStep = ActiveQuestStep };
            var ownedSession = new Objects.OwnedSession { 
                AllowJoins = true, 
                IsActive = true, 
                SkipPartyCheck = false, 
                Session = session,
            };
            ApiService.DispatchSessionStart(ownedSession);
        }

        public static void Update(int questId, byte questStep)
        {
            if (ActiveSession == null)
            {
                return;
            }
            ActiveSession.ActiveQuestId = questId;
            ActiveSession.ActiveQuestStep = questStep;
            Session!.Session = ActiveSession;
            var party = (PartyService)Plugin.GetService<PartyService>();
            var members = party.GetPartyMembers(ActiveSession);
            ApiService.DispatchUpdate(Session, members);
        }

        private void ConfigChange()
        {
            var party = (PartyService)Plugin.GetService<PartyService>();
            var members = party.GetPartyMembers(ActiveSession!);
            ApiService.DispatchConfigChange(Session!, members);
        }

        public static void UpdateParty()
        {
            if (ActiveSession == null)
            {
                return;
            }
            var party = (PartyService)Plugin.GetService<PartyService>();
            var members = party.GetPartyMembers(ActiveSession);
            ApiService.DispatchConfigChange(Session!, members);
        }

        public void SetIsActive(bool isActive)
        {
            if (Session == null || ActiveSession == null)
            {
                return;
            }
            Session.IsActive = isActive;
            ConfigChange();
        }

        public void SetAllowJoins(bool allowJoins)
        {
            if (Session == null || ActiveSession == null)
            {
                return;
            }
            Session.AllowJoins = allowJoins;
            ConfigChange();
        }

        public void SetSkipPartyCheck(bool skipPartyCheck)
        {
            if (Session == null || ActiveSession == null)
            {
                return;
            }
            Session.SkipPartyCheck = skipPartyCheck;
            ConfigChange();
        }

        public static void Cancel()
        {
            if (ActiveSession == null)
            {
                return;
            }
            var api = (ApiService)Plugin.GetService<ApiService>();
            ApiService.DispatchCancel();
        }

        internal static void Update(uint questId, byte currentStep)
        {
            Update((int)questId, currentStep);
        }
    }
}
