
using Dalamud.Plugin.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using QuestShare.Services.API;
using System.Diagnostics;

namespace QuestShare.Services
{

    internal class ApiService : IService
    {
        private HubConnection ApiConnection { get; set; } = null!;
        internal bool IsConnected => ApiConnection.State == HubConnectionState.Connected;
        internal bool IsLockedOut { get; set; } = false;
        internal HubConnectionState ConnectionState => ApiConnection.State;
        private bool isDisposing = false;
        internal static string Token => ConfigurationManager.Instance.Token;
        private readonly List<IAPIHandler> apiHandlers = [];
        private int retryCount = 0;

        public void Initialize()
        {
            var builder = new HubConnectionBuilder().WithUrl(ConfigurationManager.Instance.ApiUrl).ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Information).AddConsole();
            });
            ApiConnection = builder.Build();
            ApiConnection.Closed += async (error) =>
            {
                if (isDisposing) return;
                Log.Warning($"Connection closed... {error}");
                if (retryCount < 3)
                {
                    retryCount++;
                    await Task.Delay(new Random().Next(0, 5) * 1000);
                    await ApiConnection.StartAsync();
                } else
                {
                    Log.Error("Failed to reconnect after 3 attempts, giving up.");
                    UiService.LastErrorMessage = "Failed to reconnect to the server.";
                }
            };
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            ApiConnection.Reconnected += async (error) =>
            {
                retryCount = 0;
                Log.Information("Connection reconnected");
            };
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

            ApiConnection.On<AuthRequest.Response>(nameof(AuthRequest), AuthRequest_Client.HandleResponse);
            ApiConnection.On<Authorize.Response>(nameof(Authorize), Authorize_Client.HandleResponse);
            ApiConnection.On<Register.Response>(nameof(Register), Register_Client.HandleResponse);
            ApiConnection.On<GroupJoin.Response>(nameof(GroupJoin), GroupJoin_Client.HandleResponse);
            ApiConnection.On<GroupLeave.Response>(nameof(GroupLeave), GroupLeave_Client.HandleResponse);
            ApiConnection.On<Cancel.Response>(nameof(Cancel), Cancel_Client.HandleResponse);
            ApiConnection.On<Update.Response>(nameof(Update), Update_Client.HandleResponse);
            ApiConnection.On<Update.UpdateBroadcast>(nameof(Update.UpdateBroadcast), UpdateBroadcast_Client.HandleResponse);
            ApiConnection.On<Cancel.CancelBroadcast>(nameof(Cancel.CancelBroadcast), Cancel_Client.HandleBroadcast);
            ApiConnection.On<GroupJoin.GroupJoinBroadcast>(nameof(GroupJoin.GroupJoinBroadcast), GroupJoin_Client.HandleBroadcast);
            ApiConnection.On<GroupLeave.GroupLeaveBroadcast>(nameof(GroupLeave.GroupLeaveBroadcast), GroupLeave_Client.HandleBroadcast);
            ApiConnection.On<SessionStart.Response>(nameof(SessionStart), SessionStart_Client.HandleResponse);

            ClientState.Login += OnLogin;
            ClientState.Logout += OnLogout;
            Framework.Update += SavePersistedConfig;
            if (ConfigurationManager.Instance.ConnectOnStartup)
            {
                Task.Run(Connect);
            }
        }
        public void Shutdown()
        {
            isDisposing = true;
            if (IsConnected)
            {
                ApiConnection.StopAsync();
            }
            ApiConnection.DisposeAsync();
            ClientState.Login -= OnLogin;
            ClientState.Logout -= OnLogout;
            Framework.Update -= SavePersistedConfig;
        }

        public void OnLogin()
        {
            if (Token != "" && ConfigurationManager.Instance.ConnectOnStartup)
            {
                Task.Run(Connect);
            }
        }

        public void OnLogout(int code, int type)
        {
            ConfigurationManager.Save();
            ApiConnection.StopAsync().ConfigureAwait(false);
        }

        private void SavePersistedConfig(IFramework _)
        {
            ConfigurationManager.Instance.Token = Token;
        }

        public void OnUrlChange()
        {
            Shutdown();
            Initialize();
        }

        public async Task Connect()
        {
            Log.Debug($"Attempting to connect to {ConfigurationManager.Instance.ApiUrl}");
            try
            {
                if (IsConnected) await ApiConnection.StopAsync();
                isDisposing = false;
                await Framework.RunOnTick(async () =>
                {
                    await ApiConnection.StartAsync();
                    retryCount = 0;
                });
                
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }

        public async Task Disconnect()
        {
            isDisposing = true;
            await ApiConnection.StopAsync();
        }

        internal async Task Invoke(string methodName, object request)
        {
            if (!IsConnected) await Connect();
            Log.Debug($"Invoking {methodName} with {JsonConvert.SerializeObject(request)}");
            var s = new StackTrace();
            Log.Debug(s.ToString());
            await ApiConnection.InvokeAsync(methodName, request).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Log.Error($"Failed to invoke {methodName}: {t.Exception}");
                }
            });
        }

        public static void DispatchAuthorize() => Authorize_Client.HandleDispatch();
        public static void DispatchRegister() => Register_Client.HandleDispatch();
        public static void DispatchGroup(Objects.ShareCode shareCode) => GroupJoin_Client.HandleDispatch(shareCode);
        public static void DispatchUngroup(Objects.Session session) => GroupLeave_Client.HandleDispatch(session);
        public static void DispatchCancel() => Cancel_Client.HandleDispatch();
        public static void DispatchUpdate(Objects.OwnedSession session, List<string> partyMembers) => Update_Client.HandleDispatch(session, partyMembers, true);
        public static void DispatchConfigChange(Objects.OwnedSession session, List<string> partyMembers) => Update_Client.HandleDispatch(session, partyMembers, false);
        public static void DispatchSessionStart(Objects.OwnedSession session) => SessionStart_Client.HandleDispatch(session);

        public event EventHandler<GroupJoinEventArgs>? GroupJoined;

        internal void OnGroupJoin(GroupJoinEventArgs e)
        {
            GroupJoined?.Invoke(this, e);
        }

        public class GroupJoinEventArgs : EventArgs
        {
            public bool IsSuccess { get; set; }
            public Objects.Session? Session { get; set; }
        }
    }
}
