using ExcelDataReader;
using Playnite.SDK;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BonusTools
{
    internal class PsPrices
    {
        private const string file = "psnPrices.xlsx";
        private const string tab = "Transaction Detail";
        private BonusSettings settings;

        public PsPrices(BonusSettings settings)
        {
            this.settings = settings;
        }

        // Warning : Price will be imported in Version field !!
        // Import prices paid for games from a PlayStation data report

        // How to use :
        // 1- Follow these instructions to request your data : https://www.playstation.com/en-ca/support/account/data-request/
        // 2- You will have to wait about 24 hours for the email from Sony
        // 3- In that email, you will find a link to a file that will have this format : DataReport*.zip
        // 4- Extract the zip file
        // 5- Rename the extracted file : psnPrices.xlsx
        // 6- Place the file in the same folder as the plugin
        public void ImportPricePaid(IPlayniteAPI PlayniteApi)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (PlayniteApi.Database.BufferedUpdate())
            {
                int i = 0;
                try
                {
                    using (var stream = File.Open(file, FileMode.Open, FileAccess.Read))
                    {
                        using (var reader = ExcelReaderFactory.CreateReader(stream))
                        {
                            do
                            {
                                if (reader.Name.Contains(tab))
                                {
                                    // Read rows
                                    while (reader.Read())
                                    {

                                        // skip 3 first rows
                                        if (i < 3)
                                        {
                                            i++;
                                            continue;
                                        }

                                        // you could also import transaction date, but I don't need it
                                        //var transactionDate = reader.GetValue(0)?.ToString();

                                        // could be useful as well, to ensure not to import demos for example
                                        //var longName = reader.GetValue(2)?.ToString();

                                        var gameName = reader.GetValue(1)?.ToString();                                        
                                        var price = reader.GetValue(9)?.ToString();

                                        if (!string.IsNullOrWhiteSpace(gameName) && !string.IsNullOrWhiteSpace(price))
                                        {
                                            if (price == "0")
                                            {
                                                continue;
                                            }

                                            // since the Version field is a string, we need to format prices so it can be sorted like numbers/integers
                                            double dPrice = double.Parse(price); // 4199
                                            dPrice = dPrice / 100; // 41.99
                                            dPrice = Math.Round(dPrice, 0); // 42                                            
                                            price = dPrice.ToString("0000"); // 42 -> 0042
                                            string name = reader.GetValue(1)?.ToString();

                                            // remove special characters
                                            name = name.Replace("™", "");
                                            name = name.Replace("®", "");
                                            name = name.Replace(" PS4 & PS5", "");

                                            // TODO : it would be best to search games with the PS platforms, instead of the source since I don't use PlayStation anymore for the source
                                            var game = Levenshtein.FindBestGameMatch(name, "PlayStation", null, PlayniteApi);

                                            if (game == null)
                                            {
                                                Debug.WriteLine("NOT FOUND : " + name);

                                                // We could create a new game if not found ?
                                                //game = new Game();
                                                //game.Name = name;
                                                //game.Version = price;
                                                //game.Added = DateTime.Now;
                                                //PlayniteApi.Database.Games.Add(game);

                                                continue;
                                            }
                                            game.Version = price;
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
