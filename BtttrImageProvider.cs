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
    /// images for Movies and TV Series. Tries IMDb first; if missing and
    /// <see cref="PluginConfiguration.FallbackToTmdb"/> is enabled, falls
    /// back to the TMDB ID. Per-type enable toggles gate Movies vs Series.
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

            var url = BtttrPosterUrlBuilder.Build(idSource, idValue, configuration);
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

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(new System.Uri(url), cancellationToken);
        }
    }
}
