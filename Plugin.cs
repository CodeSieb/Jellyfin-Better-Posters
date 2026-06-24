using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.BetterPosterMinimal
{
    /// <summary>
    /// Minimal Jellyfin plugin that registers btttr.cc as a remote image provider
    /// for Movies and TV Series. IMDb-first with TMDB fallback, optional scheduled
    /// refresh, settings preview, and Reset to Defaults.
    /// </summary>
    public class Plugin : BasePlugin<Configuration.PluginConfiguration>, IHasWebPages, IPluginServiceRegistrator
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public override string Name => "Better Poster Minimal";

        public override Guid Id => Guid.Parse("c2f3aaf3-f591-4a4f-b7e2-a4f1bc9c7d1e");

        public static Plugin? Instance { get; private set; }

        /// <summary>
        /// Jellyfin 10.11.x plugin service registration entry point. The
        /// plugin host enumerates every <see cref="IPluginServiceRegistrator"/>
        /// in the plugin assembly at server startup and calls this method so
        /// we can wire our image provider and scheduled task into DI.
        /// </summary>
        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton<IRemoteImageProvider, BtttrImageProvider>();
            serviceCollection.AddSingleton<IScheduledTask, BetterPostersRefreshTask>();
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
                }
            };
        }
    }
}
