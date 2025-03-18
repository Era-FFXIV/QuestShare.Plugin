using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestShare.Services.API
{
    internal class SessionStart_Client
    {
        public static void HandleDispatch(Objects.OwnedSession session)
        {
            var api = (ApiService)Plugin.GetService<ApiService>();
            var request = new SessionStart.Request
            {
                Version = Constants.Version,
                Token = ConfigurationManager.Instance.Token,
                Session = session
            };
            _ = api.Invoke(nameof(SessionStart), request);
        }

        public static void HandleResponse(SessionStart.Response response)
        {
            if (response.Success)
            {
                Log.Information("Session started successfully");
                ConfigurationManager.Instance.OwnedSession = response.Session;
                // check if hashes match
                if (response.Session!.OwnerCharacterId != ClientState.LocalContentId.ToString().SaltedHash(response.Session.ShareCode))
                {
                    Log.Error($"Mismatched owner character ID! {response.Session!.OwnerCharacterId} != {ClientState.LocalContentId.ToString().SaltedHash(response.Session.ShareCode)}");
                }
                ConfigurationManager.Save();
                if (GameQuestManager.GetActiveQuest() != null)
                {
                    HostService.Update(GameQuestManager.GetActiveQuest()!.QuestId, GameQuestManager.GetActiveQuest()!.CurrentStep);
                } else
                {
                    HostService.UpdateParty();
                }
                UiService.LastErrorMessage = "";
            }
            else
            {
                Log.Error($"Failed to start session: {response.Error}");
                UiService.LastErrorMessage = $"Failed to start session: {response.Error}";
                _ = ((ApiService)Plugin.GetService<ApiService>()).Disconnect();
            }
        }
    }
}
