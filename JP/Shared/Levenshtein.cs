using Playnite.SDK.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BonusTools
{
    internal class Levenshtein
    {
        public static Game FindBestGameMatch(string name, string source, string platform, IPlayniteAPI PlayniteApi)
        {

            IEnumerable<Game> games;
            if (source != null)
            {
                games = PlayniteApi.Database.Games.Where(x => x.Source != null && (x.Source.Name == source));
            }
            else
            {
                games = PlayniteApi.Database.Games.Where(x => x.Platforms != null && x.Platforms.Any(p => p.Name.Equals(platform, StringComparison.OrdinalIgnoreCase)));
            }

            // if we have a near perfect match, we are done.
            var bestMatch = BestMatch(name, 95, games);
            if (bestMatch != null)
            {
                return bestMatch;
            }

            if(name == "Resident Evil 2 (2019)")
            {
                Console.WriteLine(name);
            }

            // else, find a game that contains the same name
            var contains = games.FirstOrDefault(x => x.Name != null && x.Name.Replace(" ", String.Empty).Contains(name.Replace(" ", String.Empty), StringComparison.InvariantCultureIgnoreCase));

            if (contains != null)
            {
                var distance = GetLevenshteinDistancePercent(contains.Name, name, false);
                if (distance > 25)
                {
                    return contains;
                }
            }

            // worst case scenario, let's find a close match
            return BestMatch(name, 60, games);
        }

        private static Game BestMatch(string name, int matchPercent, IEnumerable<Game> games)
        {
            var bestMatch = games
                        .Select(game => new
                        {
                            Game = game,
                            MatchPercent = GetLevenshteinDistancePercent(game.Name, name, false)
                        })
                        .OrderByDescending(x => x.MatchPercent)
                        .Where(x => x.MatchPercent > matchPercent)  // minimum threshold to avoid bad matches 
                        .FirstOrDefault();

            if (bestMatch != null && bestMatch.MatchPercent > matchPercent)  // minimum threshold to avoid bad matches
            {
                //Debug.WriteLine($"FOUND : {bestMatch.Game.Name} - Match: {bestMatch.MatchPercent}%");
            }
            else
            {
                //Debug.WriteLine($"NOT FOUND : {name}");
                return null;
            }
            return bestMatch.Game;
        }

        private static double GetLevenshteinDistancePercent(string source, string target, bool caseSensitive = true)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            {
                return 0.0;
            }

            if (!caseSensitive)
            {
                source = source.ToLower();
                target = target.ToLower();
            }

            int distance = SteamGameSearcher.LevenshteinDistance(source, target);
            int maxLength = Math.Max(source.Length, target.Length);

            return (1.0 - (double)distance / maxLength) * 100;
        }
    }
}
