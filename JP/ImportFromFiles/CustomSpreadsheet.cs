using ExcelDataReader;
using Playnite.SDK.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BonusTools
{
    // Warning : Price will be imported in Version field !!
    // Only import games that have a price

    // This class is used to import a custom spreadsheet with the price of owned games
    // The tab must be Sheet1 and the file name must be jpOwnedGames.xlsx

    // The spreadsheet must have 3 columns :
    // 1 = game name
    // 2 = platform .. must match the platform names in Playnite
    // 3 = price .. must be a whole number, no decimals
    // Price will be formatted to 4 digits, so the string can be sorted like a number
    internal class CustomSpreadsheet
    {
        private string _file = "jpOwnedGames.xlsx";
        private const string tab = "Sheet1";

        public CustomSpreadsheet(BonusSettings settings)
        {
            _file = settings.Custom;
        }

        public void ImportOwned(IPlayniteAPI PlayniteApi)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (PlayniteApi.Database.BufferedUpdate())
            {
                bool isFirstRow = true;
                try
                {
                    using (var stream = File.Open(_file, FileMode.Open, FileAccess.Read))
                    {
                        using (var reader = ExcelReaderFactory.CreateReader(stream))
                        {
                            do
                            {
                                if (reader.Name.Contains(tab))
                                {                                    
                                    while (reader.Read())
                                    {

                                        if (isFirstRow)
                                        {
                                            isFirstRow = false;
                                            continue;
                                        }

                                        var gameName = reader.GetValue(0)?.ToString();
                                        var platform = reader.GetValue(1)?.ToString();
                                        var price = reader.GetValue(2)?.ToString();

                                        if (!string.IsNullOrWhiteSpace(gameName) && !string.IsNullOrWhiteSpace(price))
                                        {
                                            if (price == "0")
                                            {
                                                continue;
                                            }

                                            // format price to 4 digits, so the string can be sorted like a number
                                            price = int.Parse(price).ToString("0000");
                                            string name = reader.GetValue(0)?.ToString();

                                            // remove special characters
                                            name = name.Replace("™", "");
                                            name = name.Replace("®", "");
                                            name = name.Replace(" PS4 & PS5", "");
                                            var game = Levenshtein.FindBestGameMatch(name, null, platform, PlayniteApi);

                                            if (game == null)
                                            {
                                                game = new Game();
                                                game.Name = name;
                                                game.Version = price;
                                                game.Added = DateTime.Now;
                                                var matchingPlatform = PlayniteApi.Database.Platforms.FirstOrDefault(p => p.Name.Equals(platform, StringComparison.OrdinalIgnoreCase));
                                                game.PlatformIds = new List<Guid> { matchingPlatform.Id };
                                                PlayniteApi.Database.Games.Add(game);
                                                //Debug.WriteLine("Added:");
                                                //Debug.WriteLine(name);
                                                continue;
                                            }
                                            if (game.Version == null || game.Version == "")
                                            {
                                                game.Version = price;
                                            }
                                            Debug.WriteLine("Updated:");
                                            Debug.WriteLine(game.Name);
                                            Debug.WriteLine(name);

                                            PlayniteApi.Database.Games.Update(game);
                                        }
                                    }
                                    break;
                                }
                            } while (reader.NextResult());
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}
