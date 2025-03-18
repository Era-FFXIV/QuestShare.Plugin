using Dalamud.Plugin.Services;

namespace QuestShare.Services
{
    internal class PartyService : IService
    {
        public void Initialize()
        {
            Framework.Update += OnFramework;
        }

        public void Shutdown()
        {
            Framework.Update -= OnFramework;
        }

        public long PartyId { get; private set; }
        private List<long> PartyMembers { get; set; } = [];

        private void OnFramework(IFramework framework)
        {
            if (PartyList.Length > 0 && PartyId == 0)
            {
                PartyId = PartyList.PartyId;
                Log.Debug($"Joined party {PartyId}");
                PartyMembers.Clear();
                foreach (var member in PartyList)
                {
                    Log.Debug($"Party member {member.Name.TextValue} - {member.ContentId}");
                    PartyMembers.Add(member.ContentId);
                }
                if (HostService.IsHost)
                {
                    HostService.UpdateParty();
                }
            }
            else if (PartyList.Length == 0 && PartyId != 0)
            {
                PartyId = 0;
                Log.Debug($"Left party");
                PartyMembers.Clear();
                if (HostService.IsHost)
                {
                    HostService.UpdateParty();
                }
            }
            else if (PartyList.Length != PartyMembers.Count)
            {
                var newMembers = PartyList.Where(x => !PartyMembers.Contains(x.ContentId)).ToList();
                var leftMembers = PartyMembers.Where(x => !PartyList.Any(y => y.ContentId == x)).ToList();
                foreach (var member in newMembers)
                {
                    Log.Debug($"Party member {member.Name.TextValue} - {member.ContentId}");
                    PartyMembers.Add(member.ContentId);
                }
                foreach (var member in leftMembers)
                {
                    Log.Debug($"Party member left {member}");
                    PartyMembers.Remove(member);
                }
                if (HostService.IsHost)
                {
                    HostService.UpdateParty();
                }
            }
        }

        public List<string> GetPartyMembers(Objects.Session session)
        {
            var members = new List<string>();
            foreach (var member in PartyList)
            {
                members.Add(member.ContentId.ToString().SaltedHash(session.ShareCode));
            }
            return members;
        }
    }
}
