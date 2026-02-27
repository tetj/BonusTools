using ExcelDataReader;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace BonusTools
{
    // Must download : https://docs.google.com/spreadsheets/d/19RorxFhWc2lHocg4c9zrVssSwZq1u2nPcpTsAvzdJQw/edit?gid=1938605355#gid=1938605355
    internal class PsMasterList
    {        
        const string PS_MONTHLY = "PS Monthly";
        const string PS_FREE = "PS Free Collection";
        const string PS_EXTRA = "PS Extra Catalogue";
        const string PS_REMOVED = "PS Extra Removed";
        const string PS_EXTRA_ICON = ".\\icons\\source_icons\\extra.png";
        const string PS_MONTHLY_ICON = ".\\icons\\source_icons\\PS Monthly.png";
        const string PS_REMOVED_ICON = ".\\icons\\removed.png";
        private BonusSettings settings;

        public PsMasterList(BonusSettings settings)
        {
            this.settings = settings;            
        }

        public void ImportStatus(IPlayniteAPI PlayniteApi)
        {
            // Register encoding provider for Excel reading
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // the big flaw with this approach is that it assumes the user has ALWAYS been a PS+ subscriber
            // eventually we could use the "added date" (from Excel) to determine if the user was a subscriber at the time of the game
            // but that would require the user to know when he/she subscribed to PS+

            var srcMonthly = PlayniteApi.Database.Sources.FirstOrDefault(t => t.Name == "PS Monthly");
            var srcExtra = PlayniteApi.Database.Sources.FirstOrDefault(t => t.Name == "PS Extra");

            using (PlayniteApi.Database.BufferedUpdate())
            {
                var tag = PlayniteApi.Database.Categories.FirstOrDefault(t => t.Name == PS_MONTHLY);
                var tier = "Playstation Plus (Discontinued)";
                SetTags(PlayniteApi, tag, tier, PS_MONTHLY_ICON, srcMonthly);

                tag = PlayniteApi.Database.Categories.FirstOrDefault(t => t.Name == PS_MONTHLY);
                tier = "Essential";
                SetTags(PlayniteApi, tag, tier, PS_MONTHLY_ICON, srcMonthly);

                tag = PlayniteApi.Database.Categories.FirstOrDefault(t => t.Name == PS_FREE);
                tier = "Essential (PS+ Collection)";                

                tag = PlayniteApi.Database.Categories.FirstOrDefault(t => t.Name == PS_EXTRA);
                tier = "Extra";
                SetTags(PlayniteApi, tag, tier, PS_EXTRA_ICON, srcExtra);

                tag = PlayniteApi.Database.Categories.FirstOrDefault(t => t.Name == PS_EXTRA);
                tier = "Extra (Ubisoft+ Classics)";
                SetTags(PlayniteApi, tag, tier, PS_EXTRA_ICON, srcExtra);
            }
        }

        private void SetTags(IPlayniteAPI PlayniteApi, Category tag, string tier, string icon, GameSource srcExtra)
        {
            var removed = PlayniteApi.Database.Categories.FirstOrDefault(t => t.Name == PS_REMOVED);

            try
            {
                using (var stream = System.IO.File.Open(settings.Master, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        do
                        {
                            if (reader.Name.Contains("Master List"))
                            {
                                int i = 0;
                                // Read rows
                                while (reader.Read())
                                {
                                    // skip 2 first rows
                                    if (i < 2)
                                    {
                                        i++;
                                        continue;
                                    }

                                    if (reader.GetValue(2)?.ToString() == tier)
                                    {
                                        var name = reader.GetValue(0)?.ToString();
                                        var system = reader.GetValue(1)?.ToString();                                        
                                        var status = reader.GetValue(3)?.ToString();

                                        if (tier == "Extra" && status == "Removed")
                                        {
                                            AssignTagToGame(PlayniteApi, removed, name, system, icon, srcExtra);
                                        }
                                        else
                                        {
                                            AssignTagToGame(PlayniteApi, tag, name, system, icon, srcExtra);
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
            return;
        }

        private static void AssignTagToGame(IPlayniteAPI PlayniteApi, Category tag, string name, string system, string icon, GameSource srcExtra)
        {
            //if (name == "Resident Evil 2 (2019)")
            //{
            //    Console.WriteLine(name);
            //}

            if (system.Contains("PS5"))
            {
                var game = Levenshtein.FindBestGameMatch(name, null, "Sony PlayStation 5", PlayniteApi);
                SetTag(PlayniteApi, tag, game, icon, srcExtra);
            }

            if (system.Contains("PS4"))
            {
                var game = Levenshtein.FindBestGameMatch(name, null, "Sony PlayStation 4", PlayniteApi);
                SetTag(PlayniteApi, tag, game, icon, srcExtra);
            }

            if (system.Contains("PS3"))
            {
                var game = Levenshtein.FindBestGameMatch(name, null, "Sony PlayStation 3", PlayniteApi);
                SetTag(PlayniteApi, tag, game, icon, srcExtra);
            }
        }

        private static void SetTag(IPlayniteAPI PlayniteApi, Category tag, Game game, string icon, GameSource srcExtra)
        {
            if (game == null)
            {
                return;
            }
            // we don't want to set free games as extra .. nor as removed !!
            if (tag.Name.Contains("Extra"))
            {
                if (game.Tags != null)
                {
                    var existingTag = game.Tags.Where(t => t.Name == PS_FREE || t.Name == PS_MONTHLY).Any();
                    if (existingTag)
                    {
                        return;
                    }
                }
            }
            
            // we don't want to set paid games as dependant on subscriptions
            if (String.IsNullOrEmpty(game.Version))
            {
                game.Categories.Add(tag);
                game.Icon = icon;
                game.SourceId = srcExtra.Id;
                PlayniteApi.Database.Games.Update(game);
            }
        }
    }
}
