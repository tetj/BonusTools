using Playnite.SDK;
using Playnite.SDK.Plugins;
using PluginsCommon;
using FlowHttp;
using SteamCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Playnite.SDK.Data;
using BonusTools.Models;
using Game = Playnite.SDK.Models.Game;
using BonusTools.JP;
using Playnite.SDK.Models;
using System.Text;
using ExcelDataReader;
using PlayniteUtilitiesCommon;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using System.Diagnostics;
using BonusTools.JP.ImportFromFiles;
using System.Runtime;


namespace BonusTools
{
    public class BonusTools : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private string steamApiLanguage;
        private const string CLIENT_ID = "____";
        private const string CLIENT_SECRET = "____";


        public override Guid Id { get; } = Guid.Parse("ca24e37a-76d9-49bf-89ab-d3cba4a54bd2");

        public BonusTools(IPlayniteAPI api) : base(api)
        {
            steamApiLanguage = "english";
        }

        private static readonly string _menuSection = "IMPORT";
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            return new List<GameMenuItem>
                {
                    new GameMenuItem
                    {
                        Description = "Update UserScore based on the numbers of reviews on Steam (IGDB*300 as fallback)",
                        MenuSection = $"{_menuSection}",                        
                        Icon = "BonusToolsUpdateIcon",
                        Action = a => {
                           RefreshGameData(args.Games);
                        }
                    },
                    new GameMenuItem
                    {                        
                        Description = "Import Playtime from Nintendo json file",
                        MenuSection = $"{_menuSection}",
                        Icon = "BonusToolsUpdateIcon",
                        Action = a => {                           
                            Nintendo.ImportPlaytime(PlayniteApi);
                        }
                    },
                    new GameMenuItem
                    {                        
                        Description = "Import PS Price Paid (in Version field) from Sony export",
                        MenuSection = $"{_menuSection}",
                        Icon = "BonusToolsUpdateIcon",
                        Action = a => {
                            PsPrices.ImportPricePaid(PlayniteApi);
                        }
                    },
                    new GameMenuItem
                    {                        
                        Description = "Import PS Price Paid (in Version field) from Custom spreadsheet",
                        MenuSection = $"{_menuSection}",
                        Icon = "BonusToolsUpdateIcon",
                        Action = a => {
                            CustomSpreadsheet.ImportOwned(PlayniteApi);
                        }
                    },
                    new GameMenuItem
                    {                        
                        Description = "Import PS Plus status from PS Plus Master List as tags",
                        MenuSection = $"{_menuSection}",
                        Icon = "BonusToolsUpdateIcon",
                        Action = a => {
                            PsMasterList.ImportStatus(PlayniteApi);
                        }
                    },
                    new GameMenuItem
                    {                        
                        Description = "Import Yuzu Compatibility as tags",
                        MenuSection = $"{_menuSection}",
                        Icon = "BonusToolsUpdateIcon",
                        Action = a => {
                            Yuzu.ImportCompatibility(PlayniteApi);
                        }
                    },
                    new GameMenuItem
                    {                        
                        Description = "Import Ryujinx Compatibility as tags",
                        MenuSection = $"{_menuSection}",
                        Icon = "BonusToolsUpdateIcon",
                        Action = a => {
                            Ryujinx.ImportCompatibility(PlayniteApi);
                        }
                    }

                };
        }

        public void RefreshGameData(List<Game> games)
        {
            // Next possible integrations :
            // - SensCritique
            // - PS Prices.com
            // - Get prices from Steam, see Valuenite

            // Update 
            Popularity.Update(games,PlayniteApi, steamApiLanguage, GetPluginUserDataPath());
            return;
        }




    }

}