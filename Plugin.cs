using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
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
    public class Plugin : BasePlugin<Configuration.PluginConfiguration>, IHasWebPages
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public override string Name => "Better Poster Minimal";

        public override Guid Id => Guid.Parse("c2f3aaf3-f591-4a4f-b7e2-a4f1bc9c7d1e");

        public static Plugin? Instance { get; private set; }

        public override void RegisterServices(IServiceCollection serviceCollection)
        {
            // Modern DI registration entry point used by Jellyfin 10.11.x.
            // The legacy IPluginServiceRegistrator path is no longer reliably
            // called, so this is the only path that wires the image
            // provider and scheduled task into Jellyfin's DI container.
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
