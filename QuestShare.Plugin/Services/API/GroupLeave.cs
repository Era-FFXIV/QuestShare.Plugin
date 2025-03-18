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
                UiService.LastErrorMessage = "";
            }
            else
            {
                Log.Error("Failed to leave the party: {0}", response.Error);
                UiService.LastErrorMessage = $"Failed to leave the party. {response.Error}";
            }
            return Task.CompletedTask;
        }

        public static void HandleBroadcast(GroupLeave.GroupLeaveBroadcast broadcast)
        {
        }
    }
}
