using System.Text.Json;
using System.IO;
using System;

namespace BonusTools
{
    public class GameStatsParser
    {
        /// <summary>
        /// Reads and parses game statistics from a JSON file
        /// </summary>
        /// <param name="filePath">Path to the JSON file</param>
        /// <returns>Parsed GameExport object</returns>
        public static GameExport? ParseGameStats(string filePath)
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
            catch (FileNotFoundException)
            {
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
        //public static void PrintGameStats(GameExport gameExport)
        //{
        //    Console.WriteLine($"Export Date: {gameExport.ExportString}");
        //    Console.WriteLine($"Version: {gameExport.ExportVersion}");

        //    foreach (var user in gameExport.Users)
        //    {
        //        Console.WriteLine($"\nUser: {user.Name} (ID: {user.Id})");
        //        foreach (var title in user.Titles)
        //        {
        //            Console.WriteLine($"\nGame: {title.Name}");
        //            Console.WriteLine($"First Played: {DateTimeOffset.FromUnixTimeSeconds(title.Summary.FirstPlayed)}");
        //            Console.WriteLine($"Last Played: {DateTimeOffset.FromUnixTimeSeconds(title.Summary.LastPlayed)}");
        //            Console.WriteLine($"Total Launches: {title.Summary.Launches}");
        //            Console.WriteLine($"Playtime (seconds): {title.Summary.Playtime}");
        //        }
        //    }
        //}
    }

}