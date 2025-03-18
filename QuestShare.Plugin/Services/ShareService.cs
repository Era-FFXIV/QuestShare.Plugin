using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using static QuestShare.Common.Objects;

namespace QuestShare.Services
{
    internal class ShareService : IService
    {
        internal static List<Objects.ShareCode> ShareCodes => ConfigurationManager.Instance.KnownShareCodes;
        internal static Dictionary<string, string> ShareCodeOwners => ConfigurationManager.Instance.ShareCodeOwners;
        internal List<Objects.Session> Sessions { get; private set; } = [];

        public void Initialize()
        {
            ClientState.Login += OnLogin;
            ClientState.Logout += OnLogout;
            Framework.Update += OnFrameworkUpdate;
        }

        public void Shutdown()
        {
            OnLogout(0, 0);
            ClientState.Login -= OnLogin;
            ClientState.Logout -= OnLogout;
            Framework.Update -= OnFrameworkUpdate;
            Sessions.Clear();
        }

        private void OnLogin()
        {
        }

        private void OnLogout(int code, int state)
        {
            Sessions.Clear();
        }

        private int pCount = 0;
        private static bool RecheckPending = false;

        public static void RecheckShareCodes()
        {
            RecheckPending = true;
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (PartyList.Count != pCount || RecheckPending)
            {
                Log.Debug("Party list changed");
                foreach (var partyMember in PartyList)
                {
                    Log.Debug($"Checking party member {partyMember.Name.TextValue} {partyMember.ContentId}");
                    foreach (var session in Sessions)
                    {
                        Log.Debug($"Checking session {session.ShareCode} - {session.OwnerCharacterId} ==? {partyMember.ContentId.ToString().SaltedHash(session.ShareCode)}");
                        if (session.OwnerCharacterId == partyMember.ContentId.ToString().SaltedHash(session.ShareCode))
                        {
                            Log.Debug($"Setting share code owner for {session.ShareCode} to {partyMember.Name.TextValue}");
                            ConfigurationManager.Instance.ShareCodeOwners[session.ShareCode] = partyMember.Name.TextValue;
                        }
                    }
                }
                pCount = PartyList.Count;
                RecheckPending = false;
            }
        }

        public static void SetActiveQuest(uint questId, byte questStep)
        {
            Log.Debug($"Setting active quest to {questId} - {questStep}");
            HostService.Update((int)questId, questStep);
        }

        public void AddSession(Objects.Session session)
        {
            if (Sessions.Any(s => s.ShareCode == session.ShareCode))
            {
                return;
            }
            Sessions.Add(session);
            AddKnownShareCode(new Objects.ShareCode() { Code = session.ShareCode, CharacterId = ClientState.LocalContentId.ToString().SaltedHash(session.ShareCode) });
        }

        public void RemoveSession(Objects.Session session)
        {
            Log.Debug($"Removing session {JsonConvert.SerializeObject(session)}");
            var existing = Sessions.FirstOrDefault(s => s.ShareCode == session.ShareCode);
            if (existing != null)
            {
                Sessions.Remove(existing);
            } else
            {
                Log.Debug("Session not found");
            }
            RemoveKnownShareCode(session.ShareCode);
        }

        public void RemoveSession(string shareCode)
        {
            
            var session = Sessions.FirstOrDefault(s => s.ShareCode == shareCode);
            if (session != null)
            {
                RemoveSession(session);
            }
        }

        public void UpdateSession(Objects.Session session)
        {
            var existing = Sessions.FirstOrDefault(s => s.ShareCode == session.ShareCode);
            if (existing != null)
            {
                Sessions.Remove(existing);
            }
            Sessions.Add(session);
        }

        public static void AddKnownShareCode(Objects.ShareCode shareCode)
        {
            if (ConfigurationManager.Instance.KnownShareCodes.Any(sc => sc.Code == shareCode.Code))
            {
                return;
            }
            ConfigurationManager.Instance.KnownShareCodes.Add(shareCode);
        }

        public static void RemoveKnownShareCode(string shareCode)
        {
            ConfigurationManager.Instance.KnownShareCodes.RemoveAll(sc => sc.Code == shareCode);
        }

        public static string GetShareCodeOwner(string shareCode)
        {
            if (ConfigurationManager.Instance.ShareCodeOwners.ContainsKey(shareCode))
            {
                return ConfigurationManager.Instance.ShareCodeOwners[shareCode];
            }
            return "Unknown";
        }
    }

}
