using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.BetterPosterMinimal.Configuration;

namespace Jellyfin.Plugin.BetterPosterMinimal
{
    /// <summary>
    /// Pure helper that builds a btttr.cc poster URL from an identifier
    /// (IMDb or TMDB) and the current plugin configuration. No I/O, no state.
    /// </summary>
    public static class BtttrPosterUrlBuilder
    {
        public const string IdSourceImdb = "imdb";
        public const string IdSourceTmdb = "tmdb";

        private const string BaseUrl = "https://btttr.cc";

        public static string Build(string idSource, string id, PluginConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(idSource))
                throw new ArgumentException("idSource must be 'imdb' or 'tmdb'.", nameof(idSource));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("id must not be empty.", nameof(id));

            var path = GetPosterPath(configuration);
            var url = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/{1}/{2}/poster-default/{3}.jpg",
                BaseUrl,
                path,
                idSource,
                Uri.EscapeDataString(id));

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

        private static string GetPosterPath(PluginConfiguration configuration)
        {
            var suffix = (configuration.EnableGenre, configuration.EnableRating) switch
            {
                (true, true) => string.Empty,
                (false, true) => "r",
                (true, false) => "g",
                (false, false) => "n"
            };

            if (configuration.EnableQualityTags)
                suffix += "q";

            if (configuration.EnableAgeRating)
                suffix += "a";

            return string.IsNullOrEmpty(suffix) ? "poster" : "poster-" + suffix;
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
