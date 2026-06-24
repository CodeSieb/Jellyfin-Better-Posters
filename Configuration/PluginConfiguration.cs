using System;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.BetterPosterMinimal.Configuration
{
    public enum PosterRatingSource
    {
        Average,
        Imdb,
        Tmdb,
        RottenTomatoes,
        Metacritic,
        Letterboxd,
        RogerEbert
    }

    public enum PosterLanguage
    {
        English,
        Spanish,
        French,
        German,
        PortugueseBrazil,
        PortuguesePortugal,
        Italian,
        Dutch,
        Polish,
        Russian,
        Turkish,
        Arabic,
        Japanese,
        Korean,
        Chinese,
        Hindi,
        Swedish,
        Czech
    }

    /// <summary>
    /// Holds the toggles that make a btttr.cc poster a "better" poster:
    /// which overlays to render (tags, ratings, genre, age rating), in
    /// which language, which item kinds to fetch for, and whether to
    /// fall back to the TMDB ID when IMDb is missing.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        // Overlay toggles.
        public bool EnableTrendTags { get; set; } = true;
        public bool EnableQualityTags { get; set; } = false;
        public bool EnableGenre { get; set; } = true;
        public bool EnableRating { get; set; } = true;
        public PosterRatingSource RatingSource { get; set; } = PosterRatingSource.Average;
        public bool EnableAgeRating { get; set; } = false;
        public PosterLanguage Language { get; set; } = PosterLanguage.English;

        // Per-item toggles.
        public bool EnableForMovies { get; set; } = true;
        public bool EnableForSeries { get; set; } = true;
        public bool EnableForSeasons { get; set; } = true;

        // When true, season variants rewrite the bare-rating path (EnableRating=on,
        // EnableGenre=off) from `poster-r` to `poster-g`. btttr.cc doesn't render
        // `poster-r/.../season:N` (it silently 404s), but it does render `poster-g`
        // for season variants with the rating absorbed into the same bottom strip.
        // Setting this on restores working rating-inclusive poster URLs for seasons
        // without changing any non-season behavior.
        public bool EnableRatingForSeasons { get; set; } = false;

        // When IMDb is missing, try TMDB. (Many series have TMDB as the primary ID.)
        public bool FallbackToTmdb { get; set; } = true;

        // Telemetry-free: stores when the plugin last successfully fetched a poster.
        // Updated whenever GetImageResponse returns a 2xx response, and whenever
        // the scheduled refresh task completes without uncaught exception.
        // Persisted to disk by BasePlugin<T> via SaveConfiguration().
        public DateTime? LastSuccessfulFetchUtc { get; set; }
    }
}
