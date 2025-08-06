using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using QuestShare.Services.API;
using System.Timers;

namespace QuestShare.Services
{

    internal class ApiService : IService
    {
        private HubConnection ApiConnection { get; set; } = null!;
        internal bool IsConnected => ApiConnection.State == HubConnectionState.Connected;
        internal bool IsLockedOut { get; set; } = false;
        internal HubConnectionState ConnectionState => ApiConnection.State;
        
        internal static string Token => ConfigurationManager.Instance.Token;
        private readonly List<IAPIHandler> apiHandlers = [];
        

        private static System.Timers.Timer? ConnectionManagerTimer = null;
        private bool WantedConnectionState;
        private int RetryCount = 0;
        private DateTime LastConnectionAttempt = DateTime.MinValue;

        private void InitializeConnectionManager()
        {
            if (ConnectionManagerTimer == null)
            {
                ConnectionManagerTimer = new System.Timers.Timer(5000);
                ConnectionManagerTimer.Elapsed += OnConnectionManagerTick;
                ConnectionManagerTimer.AutoReset = true;
                ConnectionManagerTimer.Enabled = true;
            }
        }

        private void OnConnectionManagerTick(object? source, ElapsedEventArgs e)
        {
            
            if (WantedConnectionState && ApiConnection.State == HubConnectionState.Disconnected)
            {
                if (RetryCount < 3)
                {
                    RetryCount++;
                    Log.Information($"Attempting to reconnect to {ConfigurationManager.Instance.ApiUrl} (Attempt {RetryCount})");
                    Task.Run(Connect);
                }
                else
                {
                    if (LastConnectionAttempt.AddMinutes(5) > DateTime.Now)
                    {
                        Log.Debug($"Last connection attempt was less than 5 minutes ago, skipping connection attempt.");
                        return;
                    }
                    Log.Error("Failed to reconnect after 3 attempts, giving up. Retrying in 300 seconds.");
                    UiService.LastErrorMessage = "Failed to reconnect to the server.";
                    LastConnectionAttempt = DateTime.Now;
                }
            } else
            {
                RetryCount = 0;
                LastConnectionAttempt = DateTime.MinValue;
            }
        }

        public void Initialize()
        {
            var builder = new HubConnectionBuilder().WithUrl(ConfigurationManager.Instance.ApiUrl).ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Information).AddConsole();
            });
            ApiConnection = builder.Build();
            WantedConnectionState = ConfigurationManager.Instance.ConnectOnStartup;
            ApiConnection.Closed += (error) =>
            {
                Log.Warning($"Connection closed... {error}");
                return Task.CompletedTask;
            };

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
            if (ConfigurationManager.Instance.ConnectOnStartup)
            {
                Task.Run(Connect);
            }
            InitializeConnectionManager();
        }
        public void Shutdown()
        {
            WantedConnectionState = false;
            if (IsConnected)
            {
                ApiConnection.StopAsync().ConfigureAwait(false);
            }
            ApiConnection.DisposeAsync().ConfigureAwait(false);
            ClientState.Login -= OnLogin;
            ClientState.Logout -= OnLogout;
            ConnectionManagerTimer?.Stop();
            ConnectionManagerTimer?.Dispose();
            ConnectionManagerTimer = null;
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
            WantedConnectionState = false;
            ApiConnection.StopAsync().ConfigureAwait(false);
        }

        public void OnUrlChange()
        {
            Shutdown();
            Initialize();
        }

        public async Task Connect()
        {
            if (IsConnected) return;
            Log.Debug($"Attempting to connect to {ConfigurationManager.Instance.ApiUrl}");
            try
            {
                await Framework.RunOnTick(async () =>
                {
                    await ApiConnection.StartAsync();
                    WantedConnectionState = true;
                });
                
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }

        public async Task Disconnect()
        {
            WantedConnectionState = false;
            await ApiConnection.StopAsync();
        }

        internal async Task Invoke(string methodName, object request)
        {
            if (!IsConnected) await Connect();
            Log.Debug($"Invoking {methodName} with {JsonConvert.SerializeObject(request)}");
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
