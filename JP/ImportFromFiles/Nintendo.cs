using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BonusTools.JP
{
    internal class Nintendo
    {
        private const string file = "export.json";

        // How to use :
        // 0. You must first import all your Nintendo Switch library in Playnite
        //  In order to do that, you can use nut : https://github.com/blawar/nut
        //  or Switch-Library-Manager : https://github.com/giwty/switch-library-manager
        //  You should end up with this file format : Little Kitty, Big City [01000A4019FA2000][v0][Base].nsp
        // 1. Export your playtime from your Nintendo Switch using https://github.com/zdm65477730/NX-Activity-Log
        // 2. Copy the exported file to your PC, see details here : https://cobertos.com/blog/post/nintendo-switch-activity-data-export
        // 3. Remove other users from the .json file if you have multiple users on your Switch
        // 4. Place export.json in the same folder as the plugin
        public static void ImportPlaytime(IPlayniteAPI PlayniteApi)
        {                  
            GameExport gameStats = ParseGameStats(file);
            if (gameStats == null)
            {
                Console.WriteLine("Failed to parse game stats.");
                return;
            }

            Console.WriteLine(gameStats.ExportVersion);
            int i = 0;
            foreach (var user in gameStats.Users)
            {
                foreach (var title in user.Titles)
                {
                    PlayniteApi.Database.BufferedUpdate();
                    var gameToUpdate = PlayniteApi.Database.Games.FirstOrDefault(g => g.Roms != null && g.Source != null && (g.Source.Name == "Nintendo Switch") && g.Roms.First().Name.Contains(title.Id)); // or Nintendo

                    if (gameToUpdate == null)
                    {
                        Console.WriteLine(title.Name);
                        i++;
                        continue;
                    }
                    //gameToUpdate. = DateTimeOffset.FromUnixTimeSeconds(title.Summary.FirstPlayed).DateTime;                    
                    gameToUpdate.LastActivity = DateTimeOffset.FromUnixTimeSeconds(title.Summary.LastPlayed).DateTime;
                    gameToUpdate.PlayCount = (ulong)title.Summary.Launches;
                    gameToUpdate.Playtime = (ulong)title.Summary.Playtime;
                    //Console.WriteLine($"{title.Name}: {title.FormattedFirstPlayed} {title.FormattedLastPlayed} {title.Summary.Launches}");
                    PlayniteApi.Database.Games.Update(gameToUpdate);
                }
            }

            Console.WriteLine("Count games not found : " + i);

            return;
        }

        /// <summary>
        /// Reads and parses game statistics from a JSON file
        /// </summary>
        /// <param name="filePath">Path to the JSON file</param>
        /// <returns>Parsed GameExport object</returns>
        public static GameExport ParseGameStats(string filePath)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<GameExport>(jsonString, options);
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e.Message);
                throw new FileNotFoundException($"JSON file not found at path: {filePath}");
            }
            catch (JsonException ex)
            {
                throw new JsonException($"Error parsing JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error: {ex.Message}");
            }
        }

        // Example usage of how to print game statistics
        public static void PrintGameStats(GameExport gameExport)
        {
            Console.WriteLine($"Export Date: {gameExport.ExportString}");
            Console.WriteLine($"Version: {gameExport.ExportVersion}");

            foreach (var user in gameExport.Users)
            {
                Console.WriteLine($"\nUser: {user.Name} (ID: {user.Id})");
                foreach (var title in user.Titles)
                {
                    Console.WriteLine($"\nGame: {title.Name}");
                    Console.WriteLine($"First Played: {DateTimeOffset.FromUnixTimeSeconds(title.Summary.FirstPlayed)}");
                    Console.WriteLine($"Last Played: {DateTimeOffset.FromUnixTimeSeconds(title.Summary.LastPlayed)}");
                    Console.WriteLine($"Total Launches: {title.Summary.Launches}");
                    Console.WriteLine($"Playtime (seconds): {title.Summary.Playtime}");
                }
            }
        }

    }
}
