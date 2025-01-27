using FlowHttp;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using Playnite.SDK;
using PluginsCommon;
using BonusTools.Models;
using SteamCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using Microsoft.Extensions.Logging;
using NewsViewer.Models;
using System.Xml.Linq;

//xcopy "C:\Users\JP\_CODE\darkpower\source\Generic\BonusTools\bin\Debug\IGDB.dll" "C:\Users\JP\AppData\Roaming\Playnite\Extensions\Review_Viewer_ca24e37a-76d9-49bf-89ab-d3cba4a54bd2\IGDB.dll" /y
//xcopy "C:\Users\JP\_CODE\darkpower\source\Generic\BonusTools\bin\Debug\Newtonsoft.Json.dll" "C:\Users\JP\AppData\Roaming\Playnite\Extensions\Review_Viewer_ca24e37a-76d9-49bf-89ab-d3cba4a54bd2\Newtonsoft.Json.dll" /y
//xcopy "C:\Users\JP\_CODE\darkpower\source\Generic\BonusTools\bin\Debug\RestEase.dll" "C:\Users\JP\AppData\Roaming\Playnite\Extensions\Review_Viewer_ca24e37a-76d9-49bf-89ab-d3cba4a54bd2\RestEase.dll" /y

namespace BonusTools.JP
{
    internal class Steam
    {
        public static async void Update(Game game, IPlayniteAPI PlayniteApi, string steamApiLanguage, string pluginPath)
        {
            //var userOverwriteChoice = PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCReview_Viewer_DialogOverwriteChoiceMessage"), "Steam Reviews Viewer", MessageBoxButton.YesNo);
            var reviewSearchTypes = new string[] { "all" }; // , "positive", "negative"
            var pluginDataPath = pluginPath;

            PlayniteApi.Database.BufferedUpdate();
            var gameToUpdate = PlayniteApi.Database.Games.FirstOrDefault(g => g.Id == game.Id);

            if(gameToUpdate == null)
            {
                return;
            }

            string steamId;//= await Steam.GetGameSteamIdAsync(game, game.Name, true);

            // only remove the edition if the number of reviews is below 10000
            if (gameToUpdate.UserScore < 10000)
            {
                string cleanName = IGDB.RemoveEditions(game.Name);
                steamId = await SteamCommon.Steam.GetGameSteamIdAsync(game, cleanName, true);
            }
            else
            {
                steamId = await SteamCommon.Steam.GetGameSteamIdAsync(game, game.Name, true);
            }

            //if (string.IsNullOrEmpty(steamId))
            //{
            //    gameToUpdate.UserScore = null;
            //    PlayniteApi.Database.Games.Update(gameToUpdate);
            //    return;
            //}

            foreach (string reviewSearchType in reviewSearchTypes)
            {
                var gameDataPath = Path.Combine(pluginDataPath, $"{game.Id}_{reviewSearchType}.json"); 

                // let's NOT always overwrite
                //if (!FileSystem.FileExists(gameDataPath)) // && userOverwriteChoice != MessageBoxResult.Yes
                if (true)
                {

                // we could include review bombs this way : filter_offtopic_activity = 0
                // Documentation : https://partner.steamgames.com/doc/store/getreviews            
                var reviewsApiMask = @"https://store.steampowered.com/appreviews/{0}?json=1&purchase_type=all&language=all&review_type=all";                
                var uri = string.Format(reviewsApiMask, steamId);

                // TODO JP : To prevent being rate limited
                Thread.Sleep(400);
                // this is where we call the Steam API
                HttpRequestFactory.GetHttpFileRequest().WithUrl(uri).WithDownloadTo(gameDataPath).DownloadFile();
                    // we could use DownloadStringAsync instead
                }
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

                // num_reviews - The number of reviews returned in this response
                // total_reviews - Total number of reviews matching the query parameters
                var TotalReviewsAvailable = reviews.QuerySummary.TotalReviews;

                // TODO JP : this is a hack to store the total reviews in the UserScore field!
                if (TotalReviewsAvailable > 0)
                {
                    gameToUpdate.UserScore = (int?)TotalReviewsAvailable;
                    gameToUpdate.CommunityScore = CalculateCommunityScore(reviews);
                }
                
                // TODO : find an alternative to playerCount
   
                //int playerCount = await PlayersInGameCount(steamId);
                //gameToUpdate.PlayCount = (ulong)playerCount;

                PlayniteApi.Database.Games.Update(gameToUpdate);                
            }
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
