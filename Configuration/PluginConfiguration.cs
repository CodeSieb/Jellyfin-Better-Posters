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

        // When IMDb is missing, try TMDB. (Many series have TMDB as the primary ID.)
        public bool FallbackToTmdb { get; set; } = true;
    }
}
