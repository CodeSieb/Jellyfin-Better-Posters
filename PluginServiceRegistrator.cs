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
    ///
    /// Jellyfin 10.11.x's PluginManager enumerates every type that implements
    /// <see cref="IPluginServiceRegistrator"/> inside the plugin assembly and
    /// instantiates each instance via the parameterless constructor (using
    /// <c>Activator.CreateInstance</c>). Because our <see cref="Plugin"/>
    /// extends <c>BasePlugin&lt;TConfigurationType&gt;</c> which requires
    /// <c>IApplicationPaths</c> and <c>IXmlSerializer</c> in its constructor,
    /// <c>Plugin</c> itself cannot satisfy the parameterless requirement and
    /// would crash the plugin loader with
    /// "No parameterless constructor defined". We therefore keep
    /// <see cref="IPluginServiceRegistrator"/> in this dedicated class, which
    /// has only a parameterless constructor.
    /// </summary>
    public class PluginServiceRegistrator : IPluginServiceRegistrator
    {
        /// <summary>Parameterless constructor required by Jellyfin's PluginManager Activator path.</summary>
        public PluginServiceRegistrator()
        {
        }

        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton<IRemoteImageProvider, BtttrImageProvider>();
            serviceCollection.AddSingleton<IScheduledTask, BetterPostersRefreshTask>();
        }
    }
}
