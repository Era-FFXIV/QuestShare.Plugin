global using static QuestShare.Service;
global using QuestShare.Common;
global using QuestShare.Common.API;
global using System.IO;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using QuestShare.Services;

namespace QuestShare;

public sealed class Plugin : IDalamudPlugin
{
    public static string Name => "Quest Share";
    public static string Version => "1.2.0.0";
    public static string PluginDataPath { get; private set; } = null!;
    internal static ConfigurationManager Configuration { get; private set; } = null!;
    private static ServiceProvider _serviceProvider = null!;
    internal static StringWriter LogStream { get; private set; } = null!;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>(pluginInterface);

        // Initialize configuration
        Configuration = new ConfigurationManager();

        // Initialize DI container and services
        _serviceProvider = new ServiceProvider();

        // Initialize game quest manager
        GameQuestManager.Initialize();

        // Setup debug logging
        LogStream = new StringWriter();
#if DEBUG
        Console.SetOut(LogStream);
        Console.SetError(LogStream);
#endif
        Framework.Update += OnFramework;
        Log.Debug($"Token: {ConfigurationManager.Instance.Token}");
    }

    public void Dispose()
    {
        LogStream.Dispose();

        // Shutdown all services via DI container
        _serviceProvider?.Dispose();
        GameQuestManager.Dispose();
        ConfigurationManager.Save();
        ClientState.Login -= Configuration.OnLogin;
        ClientState.Logout -= Configuration.OnLogout;
        Framework.Update -= OnFramework;
        Configuration.Dispose();
    }

    /// <summary>
    /// Get a service instance from the DI container.
    /// </summary>
    internal static T GetService<T>() where T : class, IService
    {
        return _serviceProvider.GetService<T>();
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
