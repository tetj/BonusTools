using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BonusTools
{
    internal class Nintendo
    {
        private const string file = "export.json";
        private BonusSettings settings;

        public Nintendo(BonusSettings settings)
        {
            this.settings = settings;
        }

        // How to use :
        // 0. You must first import all your Nintendo Switch library in Playnite
        //  In order to do that, you can use nut : https://github.com/blawar/nut
        //  or Switch-Library-Manager : https://github.com/giwty/switch-library-manager
        //  You should end up with this file format : Little Kitty, Big City [01000A4019FA2000][v0][Base].nsp
        // 1. Export your playtime from your Nintendo Switch using https://github.com/zdm65477730/NX-Activity-Log
        // 2. Copy the exported file to your PC, see details here : https://cobertos.com/blog/post/nintendo-switch-activity-data-export
        // 3. Remove other users from the .json file if you have multiple users on your Switch
        // 4. Place export.json in the same folder as the plugin
        public void ImportPlaytime(IPlayniteAPI PlayniteApi)
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
                    //gameToUpdate.PlayCount = (ulong)title.Summary.Launches;
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

        /// <summary>
        /// Repairs broken ROM file paths for Nintendo Switch games in the Playnite library.
        /// </summary>
        /// <remarks>
        /// For each Switch game whose ROM path no longer points to an existing file, this method
        /// extracts the game's title ID from the ROM name and searches for a file whose name
        /// contains that ID.  The search is performed in two passes:
        /// <list type="number">
        ///   <item><description>
        ///     The game's current <c>InstallDirectory</c> is searched first (non-recursive).
        ///   </description></item>
        ///   <item><description>
        ///     If nothing is found there, the fallback ROM backup directory configured in
        ///     extension settings is searched recursively across all sub-directories.
        ///     If the backup directory has not been configured the user is prompted once.
        ///   </description></item>
        /// </list>
        /// When a match is found the ROM's path is updated in the Playnite database, allowing
        /// Playnite to locate the file again without creating a duplicate library entry.
        /// </remarks>
        /// <param name="PlayniteApi">The Playnite API instance used to access the game database and show dialogs.</param>
        public void FixNintendoRomPaths(IPlayniteAPI PlayniteApi)
        {
            int fixedCount = 0;
            int notFoundCount = 0;
            int noIdCount = 0;
            int skippedCount = 0;
            bool backupWarningShown = false;

            var switchGames = PlayniteApi.Database.Games.Where(g =>
                g.Platforms != null &&
                g.Platforms.Any() &&
                g.Platforms.FirstOrDefault()?.Name == "Nintendo Switch").ToList();

            Console.WriteLine($"Found {switchGames.Count} Nintendo Switch games");

            foreach (var game in switchGames)
            {
                if (string.IsNullOrEmpty(game.InstallDirectory) || game.Roms == null || !game.Roms.Any())
                {
                    skippedCount++;
                    continue;
                }

                var rom = game.Roms.First();

                if (File.Exists(rom.Path))
                {
                    continue;
                }

                string gameId = ExtractGameId(rom.Name);
                if (string.IsNullOrEmpty(gameId))
                {
                    Debug.WriteLine($"## Could not extract game ID from: {rom.Name}");
                    noIdCount++;
                    continue;
                }

                List<string> matchingFiles = new List<string>();

                if (Directory.Exists(game.InstallDirectory))
                {
                    matchingFiles = Directory.GetFiles(game.InstallDirectory, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(f => Path.GetFileName(f).Contains(gameId, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                else
                {
                    Debug.WriteLine($"## Install directory does not exist for {game.Name}: {game.InstallDirectory}");
                }

                if (!matchingFiles.Any())
                {
                    Debug.WriteLine($"No matching file found in InstallDirectory '{game.InstallDirectory}' for '{game.Name}' (ID: {gameId}), searching backup directory...");

                    string backupDirectory = settings.NintendoRomBackup;
                    if (string.IsNullOrWhiteSpace(backupDirectory))
                    {
                        if (!backupWarningShown)
                        {
                            PlayniteApi.Dialogs.ShowMessage(
                                "ROM Backup Directory is not configured in settings.\n" +
                                "You need to specify the root directory of your ROMs.\n" +
                                "Press F9, then Extension Settings -> Generic -> Bonus Tools -> ROM Backup directory.\n",
                                "BonusTools");
                            backupWarningShown = true;
                        }
                    }
                    else if (Directory.Exists(backupDirectory))
                    {
                        try
                        {
                            matchingFiles = Directory.GetFiles(backupDirectory, "*.*", SearchOption.AllDirectories)
                                .Where(f => Path.GetFileName(f).Contains(gameId, StringComparison.OrdinalIgnoreCase)).ToList();

                            if (matchingFiles.Any())
                            {
                                Debug.WriteLine($"Found matching file in backup directory '{matchingFiles.First()}' for '{game.Name}'");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error searching backup directory for '{game.Name}': {ex.Message}");
                        }
                    }
                }

                if (matchingFiles.Any())
                {
                    PlayniteApi.Database.BufferedUpdate();
                    rom.Path = matchingFiles.First();
                    PlayniteApi.Database.Games.Update(game);
                    Console.WriteLine($"Fixed ROM path for '{game.Name}': {rom.Path}");
                    fixedCount++;
                }
                else
                {
                    Debug.WriteLine($"No matching file found for '{game.Name}' (ID: {gameId})");
                    notFoundCount++;
                }
            }

            Debug.WriteLine($"\nResults - Fixed: {fixedCount}, Not found: {notFoundCount}, No ID: {noIdCount}, Skipped: {skippedCount}");
            PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCReview_Viewer_DialogResultsDataRefreshFinishedMessage"), "SensCritiqueV2");
        }

        /// <summary>
        /// Repairs incorrect <c>InstallDirectory</c> values for Nintendo Switch games in the Playnite library.
        /// </summary>
        /// <remarks>
        /// For each Switch game whose first ROM points to an existing file, this method derives the
        /// correct install directory from that ROM's location using <see cref="Path.GetDirectoryName"/>
        /// and writes it back to <c>game.InstallDirectory</c> when it differs from the current value.
        /// Games with no ROMs, or whose first ROM path does not point to an existing file, are skipped.
        /// </remarks>
        /// <param name="PlayniteApi">The Playnite API instance used to access the game database and show dialogs.</param>
        public void FixNintendoInstallDirectories(IPlayniteAPI PlayniteApi)
        {
            int fixedCount = 0;
            int skippedCount = 0;

            var switchGames = PlayniteApi.Database.Games.Where(g =>
                g.Platforms != null &&
                g.Platforms.Any() &&
                g.Platforms.FirstOrDefault()?.Name == "Nintendo Switch").ToList();

            Console.WriteLine($"Found {switchGames.Count} Nintendo Switch games");

            foreach (var game in switchGames)
            {
                if (game.Roms == null || !game.Roms.Any())
                {
                    skippedCount++;
                    continue;
                }

                var rom = game.Roms.First();

                if (!File.Exists(rom.Path))
                {
                    skippedCount++;
                    continue;
                }

                string correctDirectory = Path.GetDirectoryName(rom.Path);

                if (string.Equals(game.InstallDirectory, correctDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                PlayniteApi.Database.BufferedUpdate();
                game.InstallDirectory = correctDirectory;
                PlayniteApi.Database.Games.Update(game);
                Console.WriteLine($"Fixed InstallDirectory for '{game.Name}': {correctDirectory}");
                fixedCount++;
            }

            Debug.WriteLine($"\nResults - Fixed: {fixedCount}, Skipped: {skippedCount}");
            PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCReview_Viewer_DialogResultsDataRefreshFinishedMessage"), "SensCritiqueV2");
        }

        private string ExtractGameId(string romName)
        {
            int startIndex = romName.IndexOf('[');
            if (startIndex == -1)
                return null;

            int endIndex = romName.IndexOf(']', startIndex);
            if (endIndex == -1)
                return null;

            string potentialId = romName.Substring(startIndex + 1, endIndex - startIndex - 1);

            if (potentialId.Length == 16 && potentialId.All(c => "0123456789ABCDEFabcdef".Contains(c)))
                return potentialId;

            return null;
        }

    }
}
