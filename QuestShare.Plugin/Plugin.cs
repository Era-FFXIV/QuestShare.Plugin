global using static QuestShare.Service;
global using QuestShare.Common;
global using QuestShare.Common.API;
global using System.IO;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using QuestShare.Services;
using Serilog.Events;

namespace QuestShare;

public sealed class Plugin : IDalamudPlugin
{
    public static string Name => "Quest Share";
    public static string Version => "1.0.1.0";
    public static string PluginDataPath { get; private set; } = null!;
    internal static ConfigurationManager Configuration { get; private set; } = null!;
    private static List<IService> Services = [];
    internal static StringWriter LogStream { get; private set; } = null!;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>(pluginInterface);
        // redirect console output to plugin log
        Configuration = new ConfigurationManager();
        Services =
        [
            new ApiService(),
            new CommandService(),
            new ShareService(),
            new PartyService(),
            new UiService(),
            new HostService(),
            new AddonService()
        ];
        GameQuestManager.Initialize();
        LogStream = new StringWriter();
#if DEBUG
        Console.SetOut(LogStream);
        Console.SetError(LogStream);
#endif
        Framework.Update += OnFramework;
        Log.Debug($"Token: {ConfigurationManager.Instance.Token}");
        foreach (var service in Services)
        {
            Log.Debug($"Initializing {service.GetType().Name}");
            service.Initialize();
        }
    }

    public void Dispose()
    {
        LogStream.Dispose();
        foreach (var service in Services)
        {
            service.Shutdown();
        }
        ConfigurationManager.Save();
        ClientState.Login -= Configuration.OnLogin;
        ClientState.Logout -= Configuration.OnLogout;
        Framework.Update -= OnFramework;
        Configuration.Dispose();
    }

    internal static IService GetService<T>() where T : IService
    {
        return Services.FirstOrDefault(s => s is T)!;
    }

    private void OnFramework(IFramework framework)
    {
#if DEBUG
        // check if there's logs to write
        if (LogStream != null && LogStream.ToString() != "")
        {
            var toWrite = LogStream.ToString();
            LogStream.GetStringBuilder().Clear();
            Log.Write(LogEventLevel.Debug, null, toWrite);
        }
#endif
    }

}
