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
                var distance = GetSimilarityScore(contains.Name, name, false);
                if (distance > 25)
                {
                    return contains;
                }
            }

            // worst case scenario, let's find a close match
            return BestMatch(name, 60, games);
        }

        public static Game BestMatch(string name, int matchPercent, IEnumerable<Game> games)
        {
            var bestMatch = games
                        .Select(game => new
                        {
                            Game = game,
                            MatchPercent = GetSimilarityScore(game.Name, name, false)
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

        public static double GetSimilarityScore(string source, string target, bool caseSensitive = false)
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

            int distance = GetDistance(source, target);
            int maxLength = Math.Max(source.Length, target.Length);

            return (1.0 - (double)distance / maxLength) * 100;
        }

        public static int GetDistance(string s1, string s2)
        {
            // Handle null or empty cases
            if (string.IsNullOrEmpty(s1))
            {
                return string.IsNullOrEmpty(s2) ? 0 : s2.Length;
            }

            if (string.IsNullOrEmpty(s2))
            {
                return s1.Length;
            }

            // Create the distance matrix
            int[,] distance = new int[s1.Length + 1, s2.Length + 1];

            // Initialize first row and column
            for (int i = 0; i <= s1.Length; i++)
            {
                distance[i, 0] = i;
            }
            for (int j = 0; j <= s2.Length; j++)
            {
                distance[0, j] = j;
            }

            // Calculate minimum operations needed
            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int cost = (s2[j - 1] == s1[i - 1]) ? 0 : 1;

                    distance[i, j] = Math.Min(
                        Math.Min(
                            distance[i - 1, j] + 1,     // Deletion
                            distance[i, j - 1] + 1),    // Insertion
                        distance[i - 1, j - 1] + cost); // Substitution
                }
            }

            // Return the minimum number of operations needed
            return distance[s1.Length, s2.Length];
        }
    }
}
