using Microsoft.Extensions.DependencyInjection;
using QuestShare.Services;

namespace QuestShare;

internal sealed class ServiceProvider : IDisposable
{
    private readonly Microsoft.Extensions.DependencyInjection.ServiceProvider _provider;
    private readonly List<IService> _services = [];

    public ServiceProvider()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _provider = services.BuildServiceProvider();
        InitializeServices();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Plugin.Configuration);
        services.AddSingleton<ApiService>();
        services.AddSingleton<CommandService>();
        services.AddSingleton<ShareService>();
        services.AddSingleton<PartyService>();
        services.AddSingleton<UiService>();
        services.AddSingleton<AddonService>();
    }

    private void InitializeServices()
    {
        // Get all registered IService implementations and initialize them
        _services.Add(_provider.GetRequiredService<ApiService>());
        _services.Add(_provider.GetRequiredService<CommandService>());
        _services.Add(_provider.GetRequiredService<ShareService>());
        _services.Add(_provider.GetRequiredService<PartyService>());
        _services.Add(_provider.GetRequiredService<UiService>());
        _services.Add(_provider.GetRequiredService<AddonService>());

        foreach (var service in _services)
        {
            Log.Debug($"Initializing {service.GetType().Name}");
            service.Initialize();
        }
    }

    public T GetService<T>() where T : class, IService
    {
        return _provider.GetRequiredService<T>();
    }

    public void Dispose()
    {
        // Shutdown services in reverse order
        for (int i = _services.Count - 1; i >= 0; i--)
        {
            var service = _services[i];
            Log.Debug($"Shutting down {service.GetType().Name}");
            service.Shutdown();
        }

        _services.Clear();
        _provider.Dispose();
    }
}
