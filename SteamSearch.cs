using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using System.Linq;
using System.Collections.Generic;


public class SteamGameSearcher
{
    private readonly HttpClient _httpClient;
    private readonly string _steamApiKey;

    public SteamGameSearcher(string steamApiKey)
    {
        _httpClient = new HttpClient();
        _steamApiKey = steamApiKey;
    }

    public class SteamGame
    {
        public int AppId { get; set; }
        public string Name { get; set; }
    }

    public async Task<(int? AppId, string GameName)> FindGameByNameAsync(string searchQuery)
    {
        try
        {
            // Encode the search query
            var encodedQuery = HttpUtility.UrlEncode(searchQuery);

            // Steam Store API endpoint for searching games
            var url = $"https://store.steampowered.com/api/storesearch/?term={encodedQuery}&l=english&cc=US";

            var response = await _httpClient.GetStringAsync(url);
            var document = JsonDocument.Parse(response);
            var root = document.RootElement;

            // Check if we have any results
            if (!root.TryGetProperty("total", out var total) || total.GetInt32() == 0)
            {
                return (null, string.Empty);
            }

            // Get the items array
            if (!root.TryGetProperty("items", out var items))
            {
                return (null, string.Empty);
            }

            var games = new List<SteamGame>();

            // Process each item in the results
            foreach (var item in items.EnumerateArray())
            {
                if (item.TryGetProperty("id", out var id) &&
                    item.TryGetProperty("name", out var name))
                {
                    games.Add(new SteamGame
                    {
                        AppId = id.GetInt32(),
                        Name = name.GetString()
                    });
                }
            }

            // Find the best match using Levenshtein distance
            var bestMatch = games
                .OrderBy(g => LevenshteinDistance(g.Name.ToLower(), searchQuery.ToLower()))
                .FirstOrDefault();

            // if distance too far && 3 first not same, return null
            var first3 = searchQuery.Length >= 4 ? searchQuery.Substring(0, 4) : searchQuery;
            first3 = first3.ToLower();
            int distance = LevenshteinDistance(bestMatch.Name.ToLower(), searchQuery.ToLower());
            if ((distance > 1) && (bestMatch.Name.ToLower().Substring(0, 4) != first3.ToLower()))
            {
                return (null, string.Empty);
            }
            else if (bestMatch != null)
            {
                return (bestMatch.AppId, bestMatch.Name);
            }
            else
            {
                return (null, string.Empty);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching for game: {ex.Message}");
            return (null, string.Empty);
        }
    }

    public static int LevenshteinDistance(string s1, string s2)
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

