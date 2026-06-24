using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.BetterPosterMinimal
{
    /// <summary>
    /// Scheduled task that walks the library and refreshes the primary
    /// image on every Movie / TV Series that the plugin is configured to
    /// cover. Default cadence: every 24 hours. Users can change it from
    /// Dashboard → Scheduled Tasks.
    /// </summary>
    public class BetterPostersRefreshTask : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IProviderManager _providerManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<BetterPostersRefreshTask> _logger;

        public BetterPostersRefreshTask(
            ILibraryManager libraryManager,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILogger<BetterPostersRefreshTask> logger)
        {
            _libraryManager = libraryManager;
            _providerManager = providerManager;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public string Name => "Better Poster - Refresh Posters";
        public string Key => "BetterPosterMinimalRefresh";
        public string Description =>
            "Refreshes primary posters from btttr.cc on every Movie and TV Series " +
            "that the plugin is configured to cover.";
        public string Category => "Better Poster";

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // Default: run once every 24 hours. Users can override this from the Dashboard.
            // TaskTriggerInfoType values differ across Jellyfin versions, so we omit Type
            // and let the framework infer "interval" from IntervalTicks.
            return new[]
            {
                new TaskTriggerInfo
                {
                    IntervalTicks = TimeSpan.FromHours(24).Ticks
                }
            };
        }

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Better Poster: starting scheduled poster refresh.");

            var configuration = Plugin.Instance?.Configuration ?? new Configuration.PluginConfiguration();

            var items = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                IsVirtualItem = false,
                Recursive = true
            });

            var filtered = new List<BaseItem>();
            foreach (var item in items)
            {
                if (item is Movie && !configuration.EnableForMovies) continue;
                if (item is Series && !configuration.EnableForSeries) continue;
                filtered.Add(item);
            }

            int total = filtered.Count;
            if (total == 0)
            {
                _logger.LogInformation("Better Poster: no Movies or Series matched enabled kinds. Skipping.");
                progress?.Report(100);
                return;
            }

            _logger.LogInformation("Better Poster: refreshing primary posters on {Total} item(s).", total);

            int done = 0;
            foreach (var item in filtered)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await _providerManager
                        .RefreshSingleItem(item, new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                        {
                            MetadataRefreshMode = MetadataRefreshMode.None,
                            ImageRefreshMode = MetadataRefreshMode.FullRefresh,
                            ReplaceAllImages = true
                        }, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Better Poster: failed to refresh poster for item {Name}.", item.Name);
                }

                done++;
                progress?.Report((double)done / total * 100.0);
            }

            _logger.LogInformation("Better Poster: scheduled refresh complete. {Done}/{Total} item(s) processed.", done, total);
        }
    }
}
