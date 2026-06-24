using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.BetterPosterMinimal.Configuration;

namespace Jellyfin.Plugin.BetterPosterMinimal
{
    /// <summary>
    /// Pure helper that builds btttr.cc poster URLs from an identifier
    /// (IMDb or TMDB) and the current plugin configuration. No I/O, no state.
    /// </summary>
    public static class BtttrPosterUrlBuilder
    {
        public const string IdSourceImdb = "imdb";
        public const string IdSourceTmdb = "tmdb";

        private const string BaseUrl = "https://btttr.cc";

        /// <summary>Builds a per-show URL with a verbatim id (IMDb or TMDB).</summary>
        public static string Build(string idSource, string id, PluginConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(idSource))
                throw new ArgumentException("idSource must be 'imdb' or 'tmdb'.", nameof(idSource));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("id must not be empty.", nameof(id));

            var path = GetPosterPath(configuration);
            return AppendQuery(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}/{1}/{2}/poster-default/{3}.jpg",
                    BaseUrl,
                    path,
                    idSource,
                    Uri.EscapeDataString(id)),
                configuration);
        }

        /// <summary>
        /// Builds a per-season URL using the parent's IMDb id and the season index.
        /// btttr.cc's path matcher expects the structure `<imdb>:season:<n>.jpg`,
        /// so we escape only the IMDb portion and concatenate the `:season:<n>`
        /// suffix verbatim rather than passing the whole id through Uri.EscapeDataString.
        /// </summary>
        public static string BuildSeason(string imdbId, int seasonNumber, PluginConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(imdbId))
                throw new ArgumentException("imdbId must not be empty.", nameof(imdbId));
            if (seasonNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(seasonNumber), seasonNumber, "Season numbers are 1-based.");

            var path = GetPosterPath(configuration);
            var url = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/{1}/{2}/poster-default/{3}:season:{4}.jpg",
                BaseUrl,
                path,
                IdSourceImdb,
                Uri.EscapeDataString(imdbId),
                seasonNumber);
            return AppendQuery(url, configuration);
        }

        public static string? GetLanguageCode(PosterLanguage language) => language switch
        {
            PosterLanguage.English => null,
            PosterLanguage.Spanish => "es",
            PosterLanguage.French => "fr",
            PosterLanguage.German => "de",
            PosterLanguage.PortugueseBrazil => "pt-BR",
            PosterLanguage.PortuguesePortugal => "pt-PT",
            PosterLanguage.Italian => "it",
            PosterLanguage.Dutch => "nl",
            PosterLanguage.Polish => "pl",
            PosterLanguage.Russian => "ru",
            PosterLanguage.Turkish => "tr",
            PosterLanguage.Arabic => "ar",
            PosterLanguage.Japanese => "ja",
            PosterLanguage.Korean => "ko",
            PosterLanguage.Chinese => "zh",
            PosterLanguage.Hindi => "hi",
            PosterLanguage.Swedish => "sv",
            PosterLanguage.Czech => "cs",
            _ => null
        };

        private static string AppendQuery(string url, PluginConfiguration configuration)
        {
            var queryParameters = new List<KeyValuePair<string, string>>();

            if (!configuration.EnableTrendTags)
                queryParameters.Add(new KeyValuePair<string, string>("tag", "none"));

            var languageCode = GetLanguageCode(configuration.Language);
            if (!string.IsNullOrEmpty(languageCode))
                queryParameters.Add(new KeyValuePair<string, string>("lang", languageCode));

            var ratingSourceCode = GetRatingSourceCode(configuration.RatingSource);
            if (configuration.EnableRating && !string.IsNullOrEmpty(ratingSourceCode))
                queryParameters.Add(new KeyValuePair<string, string>("rs", ratingSourceCode));

            if (queryParameters.Count == 0)
                return url;

            return url + "?" + string.Join("&", queryParameters.ConvertAll(p => string.Format(
                CultureInfo.InvariantCulture,
                "{0}={1}",
                Uri.EscapeDataString(p.Key),
                Uri.EscapeDataString(p.Value))));
        }

        private static string GetPosterPath(PluginConfiguration configuration)
        {
            // Path letter rule (matches btttr.cc's poster-<X> scheme; verified on
            // 2026-06-24 that EVERY user-spec pattern returns HTTP 200 from btttr.cc
            // for `tt0111161`, and that `poster-gr` returns 404 => dropping 'r' when
            // Genre is on is the correct behaviour):
            //   - 'g' = Genre overlay on (Rating piggybacks into the same bottom
            //           strip, so we omit 'r' when both Genre and Rating are on).
            //   - 'r' = Rating overlay on, Genre off (rating renders as its own strip).
            //   - 'n' = neither Genre nor Rating on (plain poster).
            //   - + 'q' = Quality tags on (4K / Dolby Vision / Atmos badges).
            //   - + 'a' = Age-rating chip on (PG-13 / TV-MA / R / etc.).
            // The path always carries at least one of {g, r, n}, so the URL never
            // collapses to a bare `poster/` when the plain poster would no longer
            // match the rendered variant on the service.
            string baseLetter =
                configuration.EnableGenre ? "g"
                : configuration.EnableRating ? "r"
                : "n";

            var suffix = baseLetter;

            if (configuration.EnableQualityTags)
                suffix += "q";

            if (configuration.EnableAgeRating)
                suffix += "a";

            return "poster-" + suffix;
        }

        private static string? GetRatingSourceCode(PosterRatingSource ratingSource) => ratingSource switch
        {
            PosterRatingSource.Average => null,
            PosterRatingSource.Imdb => "IM",
            PosterRatingSource.Tmdb => "TM",
            PosterRatingSource.RottenTomatoes => "RT",
            PosterRatingSource.Metacritic => "MC",
            PosterRatingSource.Letterboxd => "LB",
            PosterRatingSource.RogerEbert => "RE",
            _ => null
        };
    }
}
