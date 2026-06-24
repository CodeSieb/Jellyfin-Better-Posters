using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.BetterPosterMinimal.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.BetterPosterMinimal
{
    /// <summary>
    /// Remote image provider that surfaces btttr.cc posters as primary
    /// images for Movies, TV Series, and TV Seasons. Tries IMDb first;
    /// if missing and <see cref="PluginConfiguration.FallbackToTmdb"/>
    /// is enabled, falls back to the TMDB ID. Per-type enable toggles
    /// gate Movies vs Series vs Seasons.
    /// </summary>
    public class BtttrImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public const string ClientName = "BtttrPosters";

        public BtttrImageProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public string Name => "Better Poster Minimal";

        public int Order => 0;

        public bool Supports(BaseItem item)
        {
            var configuration = Plugin.Instance?.Configuration;
            if (item is Movie)
                return configuration == null || configuration.EnableForMovies;
            if (item is Series)
                return configuration == null || configuration.EnableForSeries;
            if (item is Season)
                return configuration == null || configuration.EnableForSeasons;
            return false;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
            => Supports(item) ? new[] { ImageType.Primary } : System.Array.Empty<ImageType>();

        public Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Supports(item))
                return Task.FromResult<IEnumerable<RemoteImageInfo>>(System.Array.Empty<RemoteImageInfo>());

            var configuration = Plugin.Instance?.Configuration ?? new PluginConfiguration();

            string? url = null;

            if (item is Season season)
            {
                // Per-season: the parent Series owns the IMDb ID; btttr.cc appends `:season:<n>`
                // and resolves to a season-specific poster.
                var series = season.Series;
                var imdbId = series?.GetProviderId(MetadataProvider.Imdb);
                if (string.IsNullOrWhiteSpace(imdbId) || season.IndexNumber is null or <= 0)
                {
                    return Task.FromResult<IEnumerable<RemoteImageInfo>>(System.Array.Empty<RemoteImageInfo>());
                }

                url = BtttrPosterUrlBuilder.BuildSeason(imdbId, season.IndexNumber.Value, configuration);
            }
            else
            {
                string? idSource = null;
                string? idValue = null;

                var imdbId = item.GetProviderId(MetadataProvider.Imdb);
                if (!string.IsNullOrWhiteSpace(imdbId))
                {
                    idSource = BtttrPosterUrlBuilder.IdSourceImdb;
                    idValue = imdbId;
                }
                else if (configuration.FallbackToTmdb)
                {
                    var tmdbId = item.GetProviderId(MetadataProvider.Tmdb);
                    if (!string.IsNullOrWhiteSpace(tmdbId))
                    {
                        idSource = BtttrPosterUrlBuilder.IdSourceTmdb;
                        idValue = tmdbId;
                    }
                }

                if (idSource == null || idValue == null)
                    return Task.FromResult<IEnumerable<RemoteImageInfo>>(System.Array.Empty<RemoteImageInfo>());

                url = BtttrPosterUrlBuilder.Build(idSource, idValue, configuration);
            }

            var language = BtttrPosterUrlBuilder.GetLanguageCode(configuration.Language) ?? "en";

            return Task.FromResult<IEnumerable<RemoteImageInfo>>(new[]
            {
                new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = url,
                    ThumbnailUrl = url,
                    Type = ImageType.Primary,
                    Width = 500,
                    Height = 750,
                    Language = language
                }
            });
        }

        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            var response = await _httpClientFactory
                .CreateClient(NamedClient.Default)
                .GetAsync(new Uri(url), cancellationToken)
                .ConfigureAwait(false);

            // Success hook for telemetry-free LastSuccessfulFetchUtc.
            if (response.IsSuccessStatusCode)
                StampLastSuccessfulFetchUtc();

            return response;
        }

        /// <summary>
        /// Bumps the persisted LastSuccessfulFetchUtc timestamp and saves it.
        /// Telemetry-free — no metrics ever leave the user's server.
        /// </summary>
        internal static void StampLastSuccessfulFetchUtc()
        {
            try
            {
                var instance = Plugin.Instance;
                if (instance == null) return;

                instance.Configuration.LastSuccessfulFetchUtc = DateTime.UtcNow;
                instance.SaveConfiguration();
            }
            catch
            {
                // Never let a failed timestamp write break an image fetch.
            }
        }
    }
}
