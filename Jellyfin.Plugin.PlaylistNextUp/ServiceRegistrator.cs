using Jellyfin.Plugin.PlaylistNextUp.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.PlaylistNextUp;

/// <summary>
/// Registers plugin services.
/// </summary>
public sealed class ServiceRegistrator : IPluginServiceRegistrator
{
    /// <summary>
    /// Registers services with the Jellyfin container.
    /// </summary>
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost serverApplicationHost)
    {
        serviceCollection.AddSingleton<ProgressStore>();
        serviceCollection.AddSingleton<ContainerResolver>();
        serviceCollection.AddSingleton<NextUpCalculator>();

        // Entry point
        serviceCollection.AddHostedService<EntryPoints.PlaybackMonitorEntryPoint>();
    }
}
