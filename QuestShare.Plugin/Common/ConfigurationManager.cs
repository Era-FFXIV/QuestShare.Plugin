using Newtonsoft.Json;

namespace QuestShare.Common;

public class ConfigurationManager
{
    public class Configuration
    {
        public string Token { get; set; } = string.Empty;
        public bool ConnectOnStartup { get; set; } = false;
        public bool AutoShareMsq { get; set; } = false;
        public bool AutoShareNewQuests { get; set; } = false;
        public bool HideFutureStepsHost { get; set; } = true;
        public bool HideFutureStepsMember { get; set; } = false;
        public bool EnableHosting { get; set; } = false;
        public bool HideStepsInMiniWindow { get; set; } = false;
        public Objects.OwnedSession? OwnedSession { get; set; }
        public List<Objects.ShareCode> KnownShareCodes { get; set; } = [];
        public int ActiveQuestId { get; set; } = 0;
        public byte ActiveQuestStep { get; set; } = 0;
        public Dictionary<string, string> ShareCodeOwners { get; set; } = [];
        public List<ApiConfiguration> ApiConfigurations { get; set; } = [];
        public string ApiUrl => ApiConfigurations.FirstOrDefault(x => x.Active)?.Url ?? "https://api.questshare.app/Hub";
        public string ApiDisplayName => ApiConfigurations.FirstOrDefault(x => x.Active)?.DisplayName ?? "Primary Server";
    }

    public class ApiConfiguration
    {
        public required string DisplayName;
        public required string Url;
        public bool Active = false;
    }

    public static Configuration Instance { get; private set; } = new Configuration();

    public ConfigurationManager()
    {
        Log.Debug("ConfigurationManager constructor");

        ClientState.Login += OnLogin;
        ClientState.Logout += OnLogout;
        if (PlayerState.ContentId != 0)
        {
            Load();
        }
    }

    public unsafe void OnLogin()
    {
        Load();
    }

    public void Dispose()
    {
        Save();
        ClientState.Login -= OnLogin;
        ClientState.Logout -= OnLogout;
    }

    public void OnLogout(int code, int state)
    {
        Framework.RunOnTick(Save);
    }

    public void Load()
    {
        if (File.Exists(Path.Join(PluginInterface.ConfigDirectory.FullName, $"{PlayerState.ContentId}.json")))
        {
            var config = File.ReadAllText(Path.Join(PluginInterface.ConfigDirectory.FullName, $"{PlayerState.ContentId}.json"));
            if (config != null)
            {
                var deserialized = JsonConvert.DeserializeObject<Configuration>(config);
                if (deserialized != null)
                {
                    Instance = deserialized;
                }
                else
                {
                    Log.Error($"Failed to deserialize configuration for {PlayerState.ContentId}, using defaults.");
                    Instance = new Configuration();
                    Save();
                }
            }
        }
        else
        {
            Instance = new Configuration();
            Save();
        }
    }
    public static void Save()
    {
        Log.Debug("Saving configuration");
        File.WriteAllText(Path.Join(PluginInterface.ConfigDirectory.FullName, $"{PlayerState.ContentId}.json"), JsonConvert.SerializeObject(Instance));
        Log.Debug($"Wrote config: {JsonConvert.SerializeObject(Instance)}");
    }

    
}

public enum ShareMode
{
    None,
    Host,
    Member
}
