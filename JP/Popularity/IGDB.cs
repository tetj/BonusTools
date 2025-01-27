using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace BonusTools.JP
{
    public class IGDB
    {
        // this function is used to update the UserScore and CommunityScore of a game using IGDB
        public static Task UpdateScores(Game playniteGame, IPlayniteAPI PlayniteApi)
        {
            PlayniteApi.Database.BufferedUpdate();
            var gameToUpdate = PlayniteApi.Database.Games.First(g => g.Id == playniteGame.Id);

            // TODO : we use an external program instead but I should try to import that code in the plugin directly
            string cleanName = RemoveEditions(gameToUpdate.Name);
            string output = ExecuteExternalProgram(@"C:\Users\JP\source\repos\NintendoSwitch\IGDB\bin\Debug\net8.0\IGDBexe.exe", "\"" + cleanName + "\"");
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
                if (gameToUpdate.UserScore == 0 || gameToUpdate.UserScore == null)
                {
                    gameToUpdate.UserScore = ratingCount * 300;
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
            cleanName = cleanName.Replace(" (Windows Edition)", "");
            cleanName = cleanName.Replace(" for Nintendo Switch", "");
            cleanName = cleanName.Replace(" Digital Bonus Edition", "");
            cleanName = cleanName.Replace(" (Game Preview)", "");
            cleanName = cleanName.Replace(" PS4 & PS5", "");       

            // complete                        
            cleanName = cleanName.Replace(" Console", "");
            cleanName = cleanName.Replace(" for Windows", "");                        
            cleanName = cleanName.Replace(" Collection", "");


            return cleanName;
        }

        public static string ExecuteExternalProgram(string programPath, string arguments)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = programPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return output;
                }
            }
            catch (Exception ex)
            {
                return $"Error executing program: {ex.Message}";
            }
        }

        // this was a temporary function to set UserScore = Version
        public void RefreshGameData2(List<Game> games, IPlayniteAPI PlayniteApi)
        {
            PlayniteApi.Database.BufferedUpdate();

            // for each game in the database, set user score = version
            foreach (var game in PlayniteApi.Database.Games)
            {
                if (int.TryParse(game.Version, out int versionAsInt))
                {
                    game.UserScore = versionAsInt;
                }
                else
                {
                    game.UserScore = null;
                }
                PlayniteApi.Database.Games.Update(game);
            }

            PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCReview_Viewer_DialogResultsDataRefreshFinishedMessage"), "Steam Reviews Viewer");
        }
    }
}
