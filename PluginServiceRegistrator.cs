using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.BetterPosterMinimal
{
    /// <summary>
    /// Registers the Btttr image provider and the scheduled refresh task
    /// with Jellyfin's DI container.
    /// </summary>
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton<IRemoteImageProvider, BtttrImageProvider>();
            serviceCollection.AddSingleton<IScheduledTask, BetterPostersRefreshTask>();
        }
    }
}
