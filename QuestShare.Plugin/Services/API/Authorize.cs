namespace QuestShare.Services.API
{
    internal class Authorize_Client
    {
        public static void HandleDispatch()
        {
            if (ClientState.LocalContentId == 0)
            {
                _ = ((ApiService)Plugin.GetService<ApiService>()).Disconnect();
                return;
            }
            var knownCodes = ShareService.ShareCodes.Select(s => s).ToList();
            if (HostService.ActiveSession != null)
            {
                knownCodes.Add(new Objects.ShareCode { CharacterId = HostService.ActiveSession.OwnerCharacterId, Code = HostService.ActiveSession.ShareCode });
            }
            var request = new Authorize.Request()
            {
                Token = ApiService.Token,
                Version = Constants.Version,
                ShareCodes = knownCodes,
            };
            ((ApiService)Plugin.GetService<ApiService>()).Invoke(nameof(Authorize), request).ConfigureAwait(false);
        }
        public static Task HandleResponse(Authorize.Response response)
        {
            var share = (ShareService)Plugin.GetService<ShareService>();
            if (response.Success)
            {
                ConfigurationManager.Instance.Token = response.Token;
                foreach(var session in response.Sessions)
                {
                    if (!session.IsValid)
                    {
                        Log.Warning("Session {ShareCode} is invalid.", session.ShareCode);
                        share.RemoveSession(session.ShareCode);
                        ShareService.RemoveKnownShareCode(session.ShareCode);
                        continue;
                    }
                    share.AddSession(session);
                }
                if (response.OwnedSession != null)
                {
                    ConfigurationManager.Instance.OwnedSession = response.OwnedSession;
                    if (response.OwnedSession.Session != null)
                    {
                        Log.Debug("Setting active quest to {QuestId} - {QuestStep}", response.OwnedSession.Session.ActiveQuestId, response.OwnedSession.Session.ActiveQuestStep);
                        GameQuestManager.SetActiveFlag((uint)response.OwnedSession.Session.ActiveQuestId);
                    }
                }
                ConfigurationManager.Save();
                ShareService.RecheckShareCodes();
            }
            else
            {
                Log.Error("Failed to authorize: {Error}", response.Error);
                UiService.LastErrorMessage = $"Failed to authorize: {response.Error}";
                _ = ((ApiService)Plugin.GetService<ApiService>()).Disconnect();
                if (response.Error == Error.InvalidVersion)
                {
                    UiService.LastServerMessage = "Invalid version detected, please update the plugin.";
                    ((ApiService)Plugin.GetService<ApiService>()).IsLockedOut = true;
                }
                else if (response.Error == Error.InvalidToken)
                {
                    UiService.LastServerMessage = "Invalid token detected, please reauthorize.";
                    ((ApiService)Plugin.GetService<ApiService>()).IsLockedOut = true;
                }
                else if (response.Error == Error.BannedTooManyBadRequests)
                {
                    UiService.LastServerMessage = "You are temporarily banned due to too many bad requests.";
                    ((ApiService)Plugin.GetService<ApiService>()).IsLockedOut = true;
                }
                else if (response.Error == Error.ServerMaintenance)
                {
                    UiService.LastServerMessage = "Server is currently undergoing maintenance. Please try again later.";
                }
            }
            return Task.CompletedTask;
        }
    }
}
