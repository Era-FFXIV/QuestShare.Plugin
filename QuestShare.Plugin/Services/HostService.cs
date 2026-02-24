namespace QuestShare.Services
{
    internal static class HostService
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

        public static void Start(string shareCode)
        {
            var session = new Objects.Session { OwnerCharacterId = PlayerState.ContentId.ToString().SaltedHash(shareCode), ShareCode = shareCode, ActiveQuestId = ActiveQuestId, ActiveQuestStep = ActiveQuestStep };
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
            var members = PartyService.GetPartyMembers(ActiveSession);
            ApiService.DispatchUpdate(Session, members);
        }

        private static void ConfigChange()
        {
            var members = PartyService.GetPartyMembers(ActiveSession!);
            ApiService.DispatchConfigChange(Session!, members);
        }

        public static void UpdateParty()
        {
            if (ActiveSession == null)
            {
                return;
            }
            var members = PartyService.GetPartyMembers(ActiveSession);
            ApiService.DispatchConfigChange(Session!, members);
        }

        public static void SetIsActive(bool isActive)
        {
            if (Session == null || ActiveSession == null)
            {
                return;
            }
            Session.IsActive = isActive;
            ConfigChange();
        }

        public static void SetAllowJoins(bool allowJoins)
        {
            if (Session == null || ActiveSession == null)
            {
                return;
            }
            Session.AllowJoins = allowJoins;
            ConfigChange();
        }

        public static void SetSkipPartyCheck(bool skipPartyCheck)
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
            ApiService.DispatchCancel();
        }

        internal static void Update(uint questId, byte currentStep)
        {
            Update((int)questId, currentStep);
        }
    }
}
