namespace QuestShare.Services.API
{
    internal class Update_Client
    {
        public static void HandleDispatch(Objects.OwnedSession session, List<string> partyMembers, bool broadcast = true)
        {
            if (ApiService.Token == null)
            {
                Log.Error("API Token is null. Cannot update quest status.");
                return;
            }
            var api = (ApiService)Plugin.GetService<ApiService>();
            _ = api.Invoke(nameof(Update), new Update.Request
            {
                Token = ApiService.Token,
                Version = Constants.Version,
                Session = session,
                PartyMembers = partyMembers,
                IsQuestUpdate = broadcast
            });
        }

        public static Task HandleResponse(Update.Response response)
        {
            if (response.Success)
            {
                Log.Debug("Successfully updated quest status.");
            }
            else
            {
                Log.Error("Failed to update quest status: {0}", response.Error);
                UiService.LastErrorMessage = $"Failed to update quest status. {response.Error}";
            }
            UiService.LastErrorMessage = "";
            return Task.CompletedTask;
        }
    }

    internal class UpdateBroadcast_Client
    {
        public static Task HandleResponse(Update.UpdateBroadcast response)
        {
            ((ShareService)Plugin.GetService<ShareService>()).UpdateSession(response.Session);
            return Task.CompletedTask;
        }
    }
}
