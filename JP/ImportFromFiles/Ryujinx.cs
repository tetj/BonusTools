using ExcelDataReader;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteUtilitiesCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BonusTools.ImportFromFiles
{
    internal class Ryujinx
    {
        // See meaning of comp status here : https://github.com/ryujinx-mirror/ryujinx-games-list
        // To help decide which emulator is best, see : https://docs.google.com/spreadsheets/d/1AhKo6rjQHXLuZEHPozA-F-YPejLPBrj0jbPNUyNDx78/edit?gid=77197668#gid=77197668
        // but please note this is for Steam Deck
        // for example, Bayonetta 3 appears bad on Steam Deck but looks fine according to other sources
        // best source seems to be : https://docs.google.com/spreadsheets/d/1pHPpnYmtdfS2c8KZuoMj8TYJrwfp2Yukwy3LlI2K8Ro/edit?gid=706366664#gid=706366664
        private const string file = @"_MY_FILES/Ryujinx Games List Compatibility.xlsx";
        private const string tab = "Transaction Detail";

        // How to use :
        // 1- Get Excel file from this project or https://gist.github.com/ezhevita/b41ed3bf64d0cc01269cab036e884f3d
        // 2- Place the file in the same folder as the plugin
        public static void ImportCompatibility(IPlayniteAPI PlayniteApi)
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
                                if (reader.Name.Contains("Sheet1"))
                                {
                                    // Read rows
                                    while (reader.Read())
                                    {

                                        // skip first row
                                        if (i < 1)
                                        {
                                            i++;
                                            continue;
                                        }

                                        var gameName = reader.GetValue(1)?.ToString();
                                        var titleId = reader.GetValue(2)?.ToString();
                                        var comp = "RYU-" + reader.GetValue(4)?.ToString();                                       

                                        Game game = null;
                                        if (string.IsNullOrEmpty(titleId))
                                        {
                                            game = Levenshtein.FindBestGameMatch(gameName, "Nintendo Switch", null, PlayniteApi);
                                        }
                                        else
                                        {
                                            game = PlayniteApi.Database.Games.FirstOrDefault(g => g.Roms != null && g.Source != null && (g.Source.Name == "Nintendo Switch") && g.Roms.First().Name.Contains(titleId));
                                        }

                                        if (game == null)
                                        {
                                            // it's normal, I don't have all games!
                                            //Debug.WriteLine("NOT FOUND : " + gameName);
                                            continue;
                                        }
                                        else
                                        {         
                                            if (game.Tags.Where(x => x.Name == "YUZU-Great" || x.Name == "YUZU-Perfect").FirstOrDefault() == null)
                                            {
                                                if (comp == "RYU-playable")
                                                {
                                                    PlayniteUtilities.AddTagToGame(PlayniteApi, game, comp);
                                                }
                                            }                                                                                    
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
