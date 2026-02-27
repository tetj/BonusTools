using System;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Linq;
using System.Collections.Generic;
using BonusTools;
using System.Threading;

public class GraphQLServiceV2
{
    private readonly HttpClient _httpClient;
    private const string _endpoint = "https://apollo.senscritique.com/";

    public GraphQLServiceV2()
    {
        _httpClient = new HttpClient();

        // Set Mozilla User-Agent
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public class ProductSearchResponse
    {
        [JsonPropertyName("data")]
        public SearchData Data { get; set; }
    }

    public class SearchData
    {
        [JsonPropertyName("searchProductExplorer")]
        public SearchResult SearchProductExplorer { get; set; }
    }

    public class SearchResult
    {
        [JsonPropertyName("items")]
        public ProductItem[] Items { get; set; }
    }

    public class ProductItem
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("alternativeTitles")]
        public string[] AlternativeTitles { get; set; }

        [JsonPropertyName("originalTitle")]
        public string OriginalTitle { get; set; }

        [JsonPropertyName("rating")]
        public decimal? Rating { get; set; }

        [JsonPropertyName("stats")]
        public ProductStats Stats { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        public double bestMatchScore;

        public string GetFormattedTitles()
        {
            var titles = new StringBuilder();
            titles.AppendLine($"Main Title: {Title}");

            if (!string.IsNullOrEmpty(OriginalTitle) && OriginalTitle != Title)
            {
                titles.AppendLine($"Original Title: {OriginalTitle}");
            }

            if (AlternativeTitles?.Any() == true)
            {
                titles.AppendLine($"Alternative Titles: {string.Join(", ", AlternativeTitles)}");
            }

            return titles.ToString().TrimEnd();
        }

        public double GetBestMatchScore(string searchTitle)
        {
            //if(bestMatchScore != 0)
            //{
            //    return bestMatchScore;
            //}

            if (string.IsNullOrWhiteSpace(searchTitle))
                return 0;

            var scores = new List<double>();

            // Check main title
            scores.Add(Levenshtein.GetSimilarityScore(Title ?? "", searchTitle));

            // Check original title
            if (!string.IsNullOrEmpty(OriginalTitle))
            {
                scores.Add(Levenshtein.GetSimilarityScore(OriginalTitle, searchTitle));
            }

            // Check alternative titles
            if (AlternativeTitles?.Any() == true)
            {
                scores.AddRange(AlternativeTitles
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Select(t => Levenshtein.GetSimilarityScore(t, searchTitle)));
            }

            bestMatchScore = scores.Max();
            return bestMatchScore;
        }
    }

    public class ProductStats
    {
        [JsonPropertyName("ratingCount")]
        public int? RatingCount { get; set; }
    }

    public async Task<ProductItem> FindClosestMatchAsync(string searchTitle, string query)
    {
        var items = await SearchProductAsync(query);
        searchTitle = BonusTools.IGDB.RemoveEditions(searchTitle);

        if (items == null ||
            !items.Any())
        {
            return null;
        }

        return items
            .Select(item => new
            {
                Item = item,
                Score = item.GetBestMatchScore(searchTitle)
            })
            .OrderByDescending(x => x.Score)
            .First().Item;
    }

    public async Task<ProductItem[]> SearchProductAsync(string query)
    {
        Thread.Sleep(400);
        var request = new
        {
            query = query
        };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        try
        {
            var response = await _httpClient.PostAsync(_endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = System.Text.Json.JsonSerializer.Deserialize<ProductSearchResponse>(responseContent);

            return result?.Data?.SearchProductExplorer?.Items;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to execute GraphQL query: {ex.Message}", ex);
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new Exception($"Failed to parse GraphQL response: {ex.Message}", ex);
        }
    }
}