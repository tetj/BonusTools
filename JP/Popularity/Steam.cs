using Playnite.SDK.Data;
using Playnite.SDK.Models;
using Playnite.SDK;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using FlowHttp;

namespace BonusTools
{
    internal class Steam
    {
        public static void Update(Game game, IPlayniteAPI PlayniteApi, string steamApiLanguage, string pluginPath)
        {            
            var reviewSearchTypes = new string[] { "all" }; // , "positive", "negative"
            var pluginDataPath = pluginPath;
            var gameToUpdate = PlayniteApi.Database.Games.FirstOrDefault(g => g.Id == game.Id);

            if(gameToUpdate == null)
            {
                return;
            }

            string cleanName = IGDB.RemoveEditions(game.Name);
            string steamId = SteamCommon.Steam.GetGameSteamIdAsync(game, cleanName, true).GetAwaiter().GetResult();

            if(steamId == null)
            {
                return;
            }
            foreach (string reviewSearchType in reviewSearchTypes)
            {
                Thread.Sleep(400);
                string gameDataPath = GetSteamReviews(game, pluginDataPath, steamId, reviewSearchType);
                UpdatePlayCount(PlayniteApi, gameToUpdate, gameDataPath);                
            }
        }

        private static string GetSteamReviews(Game game, string pluginDataPath, string steamId, string reviewSearchType)
        {
            var gameDataPath = Path.Combine(pluginDataPath, $"{game.Id}_{reviewSearchType}.json");
            // if we don't want to overwrite the file, we could add that condition
            //if (!FileSystem.FileExists(gameDataPath)) // && userOverwriteChoice != MessageBoxResult.Yes
            var reviewsApiMask = @"https://store.steampowered.com/appreviews/{0}?json=1&purchase_type=all&language=all&review_type=all";
            var uri = string.Format(reviewsApiMask, steamId);
            HttpRequestFactory.GetHttpFileRequest().WithUrl(uri).WithDownloadTo(gameDataPath).DownloadFile(); // this is where we call the Steam API
            return gameDataPath;
        }

        private static void UpdatePlayCount(IPlayniteAPI PlayniteApi, Game gameToUpdate, string gameDataPath)
        {
            Serialization.TryFromJsonFile<ReviewsResponse>(gameDataPath, out var data);
            if (data == null)
            {
                return;
            }

            ReviewsResponse reviews = data;

            if (reviews.QuerySummary == null)
            {
                return;
            }

            var TotalReviewsAvailable = reviews.QuerySummary.TotalReviews;

            if (TotalReviewsAvailable > 0)
            {
                gameToUpdate.PlayCount = (ulong)TotalReviewsAvailable;
                gameToUpdate.CommunityScore = CalculateCommunityScore(reviews);
            }

            PlayniteApi.Database.Games.Update(gameToUpdate);
        }

        private static int CalculateCommunityScore(ReviewsResponse reviews)
        {
            var totalVotes = reviews.QuerySummary.TotalPositive + reviews.QuerySummary.TotalNegative;
            double average = (double)reviews.QuerySummary.TotalPositive / (double)totalVotes;
            double score = average - (average - 0.5) * Math.Pow(2, -Math.Log10(totalVotes + 1));
            return Convert.ToInt32(score * 100);
        }

        private static async Task<int> PlayersInGameCount(string steamId)
        {
            string steamApiCurrentPlayersMask = @"https://api.steampowered.com/ISteamUserStats/GetNumberOfCurrentPlayers/v1/?appid={0}";

            try
            {
                // Build the API URL
                var url = string.Format(steamApiCurrentPlayersMask, steamId);

                // Fetch player count data from Steam API
                var response = await HttpRequestFactory
                    .GetHttpRequest()
                    .WithUrl(url)
                    .DownloadStringAsync();

                // Parse the response
                if (!Serialization.TryFromJson<NumberOfPlayersResponse>(response.Content, out var playerData))
                {
                    Console.WriteLine($"Failed to parse player count data for game {steamId}");
                    return 0;
                }

                // Validate API response
                if (playerData.Response.Result != 1)
                {
                    Console.WriteLine($"Invalid API response for game {steamId}. Result: {playerData.Response.Result}");
                    return 0;
                }

                // Cache the player count data
                //var cachedData = playersCountCacheManager.Add(contextGameId, playerData);

                if (playerData?.Response != null)
                {
                    return playerData.Response.PlayerCount;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating player count for game {steamId}: {ex.Message}");
            }

            return 0;
        }

    }
}
