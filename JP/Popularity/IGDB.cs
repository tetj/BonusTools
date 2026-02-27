using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace BonusTools
{
    public class IGDB
    {
        private const double RATING_COUNT_MULTIPLIER = 300;

        // this function is used to update the PlayCount and CommunityScore of a game using IGDB
        public static Task UpdateScores(Game playniteGame, IPlayniteAPI PlayniteApi, BonusSettings settings)
        {            
            var gameToUpdate = PlayniteApi.Database.Games.First(g => g.Id == playniteGame.Id);

            string cleanName = RemoveEditions(gameToUpdate.Name);

            //string output = IGDBClientHelperV2.FetchGameRatings(cleanName);
            string output = new IGDBClientHelperV2(settings).SearchByName(cleanName);
            if(output == null)
            {
                return Task.CompletedTask;
            }
            string[] results = output.Trim().Split('|');

            // Try to parse the sum
            if (!double.TryParse(results[0], out double rating))
            {
                Console.WriteLine($"Failed to parse sum value: {results[0]}");
            }

            // Try to parse the average
            if (!int.TryParse(results[1], out int ratingCount))
            {
                Console.WriteLine($"Failed to parse average value: {results[1]}");
            }

            // update the Playnite game with the IGDB data
            if (ratingCount != 0)
            {
                if (gameToUpdate.PlayCount == 0)
                {
                    gameToUpdate.PlayCount = (ulong)(ratingCount * RATING_COUNT_MULTIPLIER);
                    // 300 = to compensate with the Steam reviews since IGDB is a lot less popular
                    // this is an arbitrary value, it could probably be higher a bit                    
                }
                if (gameToUpdate.CommunityScore == 0 || gameToUpdate.CommunityScore == null)
                {
                    gameToUpdate.CommunityScore = (int)Math.Round(rating, 0);
                }
            }

            PlayniteApi.Database.Games.Update(gameToUpdate);
            return Task.CompletedTask;
        }

        public static string RemoveEditions(string name)
        {
            string cleanName = name.Replace(": Windows Edition", "");           
            cleanName = cleanName.Replace(" (Windows Version)", "");
            cleanName = cleanName.Replace(" - Standard Edition", "");
            cleanName = cleanName.Replace(" - Ultimate Edition", "");
            cleanName = cleanName.Replace(" - Windows Edition", "");
            cleanName = cleanName.Replace(" - Limited Edition", "");
            cleanName = cleanName.Replace(" - Gold Edition", "");
            cleanName = cleanName.Replace(" - Special Edition", "");
            cleanName = cleanName.Replace(" - Definitive Edition", "");
            cleanName = cleanName.Replace(" - Collector's Edition", "");
            cleanName = cleanName.Replace(" - Base Game", "");
            cleanName = cleanName.Replace(" - PC Edition", "");
            cleanName = cleanName.Replace(" - Complete Edition", "");
            cleanName = cleanName.Replace(" - Complete Bundle", "");
            cleanName = cleanName.Replace(" - Game of the Year Edition", "");
            cleanName = cleanName.Replace(" - Nintendo Switch Edition", "");            

            cleanName = cleanName.Replace(": Complete Edition", "");
            cleanName = cleanName.Replace(": Challenger Edition", "");
            cleanName = cleanName.Replace(": The Complete Edition", "");
            cleanName = cleanName.Replace(" Game of the Year Edition", "");
            cleanName = cleanName.Replace(" GAME OF THE YEAR EDITION", "");

            cleanName = cleanName.Replace(" Standard Edition", "");            
            cleanName = cleanName.Replace(" Ultimate Edition", "");            
            cleanName = cleanName.Replace(" Windows Edition", "");
            cleanName = cleanName.Replace(" Limited Edition", "");            
            cleanName = cleanName.Replace(" Gold Edition", "");
            cleanName = cleanName.Replace(" Special Edition", "");
            cleanName = cleanName.Replace(" Definitive Edition", "");            
            cleanName = cleanName.Replace(" Collector's Edition", "");
            cleanName = cleanName.Replace(" Base Game", "");
            cleanName = cleanName.Replace(" PC Edition", "");
            cleanName = cleanName.Replace(" Complete Edition", "");
            cleanName = cleanName.Replace(" Complete Bundle", "");
            cleanName = cleanName.Replace(" Classic Edition", "");
            cleanName = cleanName.Replace(" Console Edition", "");
            cleanName = cleanName.Replace(" Enhanced Edition", "");
            cleanName = cleanName.Replace(" Deluxe Edition", "");
            cleanName = cleanName.Replace(" Legacy Edition", "");
            cleanName = cleanName.Replace(" Challenger Edition", "");
            cleanName = cleanName.Replace(" Collector's Edition", "");
            cleanName = cleanName.Replace(" (Windows Edition)", "");
            cleanName = cleanName.Replace(" for Nintendo Switch", "");
            cleanName = cleanName.Replace(" Digital Bonus Edition", "");
            cleanName = cleanName.Replace(" (Game Preview)", "");
            cleanName = cleanName.Replace(" PS4 & PS5", "");       

            // complete                        
            cleanName = cleanName.Replace(" Console", "");
            cleanName = cleanName.Replace(" for Windows", "");                        
            cleanName = cleanName.Replace(" Collection", "");
            cleanName = cleanName.Replace(" Edition", "");

            return cleanName;
        }

        // this was a temporary function to invert fields in the database
        public static void RefreshGameData2(List<Game> games, IPlayniteAPI PlayniteApi)
        {
            PlayniteApi.Database.BufferedUpdate();

            // for each game in the database, set user score = version
            foreach (var game in games)
            {
                try
                {
                    var userScore = game.UserScore;
                    game.UserScore = (int?)game.PlayCount;
                    game.PlayCount = (ulong)userScore;
                    game.Modified = DateTime.Now;
                    PlayniteApi.Database.Games.Update(game);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Debug.WriteLine(game.Name);
                }

                //Thread.Sleep(100);
            }

            PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCReview_Viewer_DialogResultsDataRefreshFinishedMessage"), "TEMP INVERTER");
        }
    }
}
