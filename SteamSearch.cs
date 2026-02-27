using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using System.Linq;
using System.Collections.Generic;
using BonusTools;


public class SteamGameSearcher
{
    private readonly HttpClient _httpClient;

    public SteamGameSearcher()
    {
        _httpClient = new HttpClient();
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
            searchQuery = searchQuery.Replace(" ", "*");
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
                .OrderBy(g => Levenshtein.GetDistance(g.Name.ToLower(), searchQuery.ToLower()))
                .FirstOrDefault();

            // TODO : remove that condition ? not really useful
            // similarity matters less if first characters matches
            var first5 = searchQuery.Length >= 5 ? searchQuery.Substring(0, 5) : searchQuery;
            first5 = first5.ToLower();
            bool sameFirst5 = bestMatch.Name.ToLower().Substring(0, 5) == first5.ToLower();

            double similarity = Levenshtein.GetSimilarityScore(bestMatch.Name.ToLower(), searchQuery.ToLower());
            if (bestMatch != null && similarity > 60 && sameFirst5)
            {
                return (bestMatch.AppId, bestMatch.Name);
            }
            else if (bestMatch != null && similarity > 70)
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


}

