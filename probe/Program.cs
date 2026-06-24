// Regression probe for BtttrPosterUrlBuilder.Build().
//
// Builds the parent plugin DLL via ProjectReference, then invokes
// BtttrPosterUrlBuilder.Build() with each of the seven URL patterns the user
// specified in https://btttr.cc/configure and asserts the produced URL
// matches verbatim. Exit 0 = all pass; non-zero = at least one mismatch.
//
// Run from this directory with:
//     dotnet run -c Release
//
// Verified live against btttr.cc on 2026-06-24 that each pattern below
// returns HTTP 200 for `tt0111161` (Shawshank Redemption).
//
// Uses a traditional class-based entry point (`static int Main`) instead of
// top-level statements: with the probe `<ProjectReference>`-ing a sibling
// class library under the same solution, the dotnet 9 SDK's auto-detection
// of the entry point can flip to "library" on the probe project and emit
// CS8805 even when `<OutputType>Exe</OutputType>` is set. Declaring the
// entry point explicitly via `Main` short-circuits that detection entirely.

using System;
using Jellyfin.Plugin.BetterPosterMinimal;
using Jellyfin.Plugin.BetterPosterMinimal.Configuration;

namespace Jellyfin.Plugin.BetterPosterMinimal.Probe
{
    internal static class Program
    {
        private const string ImdbId = "tt0111161";

        private static int Main(string[] args)
        {
            var cases = new (string Name, PluginConfiguration Cfg, string Expected)[]
            {
                (
                    "normal English",
                    new PluginConfiguration
                    {
                        EnableTrendTags = false, EnableQualityTags = false,
                        EnableGenre = false, EnableRating = false, EnableAgeRating = false
                    },
                    "https://btttr.cc/poster-n/imdb/poster-default/tt0111161.jpg?tag=none"
                ),
                (
                    "with trend tag",
                    new PluginConfiguration
                    {
                        EnableTrendTags = true, EnableQualityTags = false,
                        EnableGenre = false, EnableRating = false, EnableAgeRating = false
                    },
                    "https://btttr.cc/poster-n/imdb/poster-default/tt0111161.jpg"
                ),
                (
                    "with quality tag",
                    new PluginConfiguration
                    {
                        EnableTrendTags = true, EnableQualityTags = true,
                        EnableGenre = false, EnableRating = false, EnableAgeRating = false
                    },
                    "https://btttr.cc/poster-nq/imdb/poster-default/tt0111161.jpg"
                ),
                (
                    "with genre tag",
                    new PluginConfiguration
                    {
                        EnableTrendTags = false, EnableQualityTags = false,
                        EnableGenre = true, EnableRating = false, EnableAgeRating = false
                    },
                    "https://btttr.cc/poster-g/imdb/poster-default/tt0111161.jpg?tag=none"
                ),
                (
                    "with rating tag",
                    new PluginConfiguration
                    {
                        EnableTrendTags = false, EnableQualityTags = false,
                        EnableGenre = false, EnableRating = true, EnableAgeRating = false
                    },
                    "https://btttr.cc/poster-r/imdb/poster-default/tt0111161.jpg?tag=none"
                ),
                (
                    "with age rating",
                    new PluginConfiguration
                    {
                        EnableTrendTags = false, EnableQualityTags = false,
                        EnableGenre = false, EnableRating = false, EnableAgeRating = true
                    },
                    "https://btttr.cc/poster-na/imdb/poster-default/tt0111161.jpg?tag=none"
                ),
                (
                    "all combined",
                    new PluginConfiguration
                    {
                        EnableTrendTags = true, EnableQualityTags = true,
                        EnableGenre = true, EnableRating = true, EnableAgeRating = true
                    },
                    "https://btttr.cc/poster-gqa/imdb/poster-default/tt0111161.jpg"
                ),
            };

            int pass = 0;
            int fail = 0;

            Console.WriteLine("Regression probe for BtttrPosterUrlBuilder.Build()");
            Console.WriteLine(new string('-', 72));

            foreach (var (name, cfg, expected) in cases)
            {
                var actual = BtttrPosterUrlBuilder.Build("imdb", ImdbId, cfg);
                var ok = actual == expected;
                if (ok) pass++; else fail++;

                Console.Write("[");
                Console.Write(ok ? "PASS" : "FAIL");
                Console.Write("]  ");
                Console.Write(name.PadRight(20));
                Console.Write("  ->  ");
                Console.WriteLine(actual);

                if (!ok)
                {
                    Console.Write("        expected: ");
                    Console.WriteLine(expected);
                }
            }

            Console.WriteLine(new string('-', 72));
            Console.Write($"{pass}/{cases.Length} passed");
            if (fail == 0)
            {
                Console.WriteLine(". OK");
                return 0;
            }

            Console.WriteLine($". {fail} FAILED.");
            return 1;
        }
    }
}
