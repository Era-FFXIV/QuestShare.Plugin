namespace QuestShare.Services.API
{
    internal class GroupJoin_Client
    {

        public static void HandleDispatch(Objects.ShareCode shareCode) {
            var api = (ApiService)Plugin.GetService<ApiService>();
            var request = new GroupJoin.Request()
            {
                Token = ApiService.Token,
                Version = Constants.Version,
                SessionInfo = shareCode
            };
            api.Invoke(nameof(GroupJoin), request).ConfigureAwait(false);
        }

        public static Task HandleResponse(GroupJoin.Response resp)
        {
            if (resp.Success && resp.Session != null)
            {
                var share = (ShareService)Plugin.GetService<ShareService>();
                Log.Information("Successfully joined group.");
                share.AddSession(resp.Session);
                ShareService.RecheckShareCodes();
                var api = (ApiService)Plugin.GetService<ApiService>();
                api.OnGroupJoin(new ApiService.GroupJoinEventArgs { Session = resp.Session, IsSuccess = true });
                UiService.LastErrorMessage = "";
            }
            else
            {
                Log.Error("Failed to join group: {Error}", resp.Error);
                var api = (ApiService)Plugin.GetService<ApiService>();
                api.OnGroupJoin(new ApiService.GroupJoinEventArgs { Session = null, IsSuccess = false });
                var friendlyError = resp.Error switch
                {
                    Error.InvalidVersion => "Invalid version",
                    Error.InvalidToken => "Invalid token",
                    Error.InvalidParty => "Code is incorrect or you are not in the host's party.",
                    Error.Unauthorized => "Unauthorized, please reconnect.",
                    Error.InvalidSession => "Session invalid, incorrect code or host has disabled joins.",
                    _ => "Unknown error"
                };
                UiService.LastErrorMessage = $"Failed to join group. {friendlyError}";
            }
            return Task.CompletedTask;
        }

        public static Task HandleBroadcast(GroupJoin.GroupJoinBroadcast broadcast)
        {
            var share = (ShareService)Plugin.GetService<ShareService>();
            share.AddSession(broadcast.Session);
            return Task.CompletedTask;
        }
    }
}
