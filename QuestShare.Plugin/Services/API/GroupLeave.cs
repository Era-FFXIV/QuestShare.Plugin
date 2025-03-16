namespace QuestShare.Services.API
{
    internal class GroupLeave_Client
    {
        public static void HandleDispatch(Objects.Session session)
        {
            var api = (ApiService)Plugin.GetService<ApiService>();
            var request = new GroupLeave.Request
            {
                Token = ApiService.Token,
                Version = Constants.Version,
                Session = session
            };
            api.Invoke(nameof(GroupLeave), request).ConfigureAwait(false);
        }

        public static Task HandleResponse(GroupLeave.Response response)
        {
            if (response.Success && response.Session != null)
            {
                var share = (ShareService)Plugin.GetService<ShareService>();
                share.RemoveSession(response.Session);
            }
            else
            {
                UiService.LastErrorMessage = "Failed to leave the party.";
            }
            return Task.CompletedTask;
        }

        public static void HandleBroadcast(GroupLeave.GroupLeaveBroadcast broadcast)
        {
            Log.Debug($"[GroupLeave] {broadcast.Session.OwnerCharacterId} left the party.");
        }
    }
}
