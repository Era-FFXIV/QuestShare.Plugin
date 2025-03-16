namespace QuestShare.Services.API
{
    internal class Cancel_Client
    {
        public static void HandleDispatch()
        {
            var api = (ApiService)Plugin.GetService<ApiService>();
            var party = (PartyService)Plugin.GetService<PartyService>();
            var share = (ShareService)Plugin.GetService<ShareService>();
            var request = new Cancel.Request
            {
                Token = ApiService.Token,
                Version = Constants.Version,
                ShareCode = HostService.ActiveSession!.ShareCode,
                OwnerCharacterId = HostService.ActiveSession!.OwnerCharacterId
            };
            _ = api.Invoke(nameof(Cancel), request);
        }

        public static Task HandleResponse(Cancel.Response cancelResponse)
        {
            if (cancelResponse.Success)
            {
                var share = (ShareService)Plugin.GetService<ShareService>();
                ConfigurationManager.Instance.OwnedSession = null;
            }
            else
            {
                UiService.LastErrorMessage = "Failed to cancel the party.";
            }
            return Task.CompletedTask;
        }

        public static Task HandleBroadcast(Cancel.CancelBroadcast cancelBroadcast)
        {
            var share = (ShareService)Plugin.GetService<ShareService>();
            share.RemoveSession(cancelBroadcast.ShareCode);
            return Task.CompletedTask;
        }
    }
}
