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
    internal class Yuzu
    {
        private const string file = "_MY_FILES/yuzu-comp.xlsx";
        private const string tab = "Sheet1";

        // How to use :        
        // 1- Get Excel file from this project or https://www.reddit.com/r/yuzu/comments/tacr43/yuzu_game_compatibility_list_sorted_by_the/
        // 2- Place the file in the same folder as the plugin
        // 3- See meaning of tags here : https://web.archive.org/web/20240229065115/https://yuzu-emu.org/game/
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
                                if (reader.Name.Contains(tab))
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

                                        var gameName = reader.GetValue(0)?.ToString();                                        
                                        var comp = "YUZU-" + reader.GetValue(2)?.ToString();

                                        var game = Levenshtein.FindBestGameMatch(gameName, "Nintendo Switch", null, PlayniteApi);

                                        if (game == null)
                                        {
                                            // it's normal, I don't have all games!
                                            //Debug.WriteLine("NOT FOUND : " + gameName);
                                            continue;
                                        }
                                        else
                                        {
                                            PlayniteUtilities.AddTagToGame(PlayniteApi, game, comp);
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
