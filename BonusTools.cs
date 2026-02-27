using BonusTools.ImportFromFiles;
using BonusTools.JP;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;


namespace BonusTools
{
    public class BonusTools : GenericPlugin
    {
        private string steamApiLanguage;
        private BonusSettingsViewModel settings { get;  set; }        

        public override Guid Id { get; } = Guid.Parse("ca24e37a-76d9-49bf-89ab-d3cba4a54bd2");

        public BonusTools(IPlayniteAPI api) : base(api)
        {
            settings = new BonusSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };

            AddSettingsSupport(new AddSettingsSupportArgs
            {
                SourceName = "BonusTools",
                SettingsRoot = $"{nameof(settings)}.{nameof(settings.Settings)}"
            });

            // TODO : 
            // var steamApiLanguage = Steam.GetSteamApiMatchingLanguage(PlayniteApi.ApplicationSettings.Language);
            steamApiLanguage = "english";
        }

        private static readonly string _menuSection = "BonusTools";
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            return new List<GameMenuItem>
                {
                    new GameMenuItem
                    {
                        Description = "Update PlayCount based on the numbers of reviews on Steam/SensCritique/IGDB",
                        MenuSection = $"{_menuSection}",                        
                        Icon = "BonusToolsUpdateIcon",
                        Action = a => {
                           Popularity.UpdatePlayCount(args.Games, PlayniteApi, steamApiLanguage, GetPluginUserDataPath(), settings.Settings);
                        }
                    },
                    new GameMenuItem
                    {
                        Description = "Import SensCritique.com data (OVERWRITE UserScore and PlayCount, unless disabled via settings)",
                        MenuSection = $"{_menuSection}",
                        Icon = "BonusToolsUpdateIcon",
                        Action = a => {
                            new SensCritiqueV2(settings.Settings).UpdateAll(PlayniteApi, args.Games);
                        }
                    },
                    new GameMenuItem
                    {
                        Description = "Count # of games selected",
                        MenuSection = $"{_menuSection}",
                        Icon = "BonusToolsUpdateIcon",
                        Action = a => {
                            new SensCritiqueV2(settings.Settings).Count(PlayniteApi, args.Games);
                        }
                    },
                    new GameMenuItem
                    {
                        Description = "Update roms based on Nintendo IDs",
                        MenuSection = $"{_menuSection}",
                        Icon = "BonusToolsUpdateIcon",
                        Action = a => {
                            new Nintendo(settings.Settings).FixNintendoRomPaths(PlayniteApi);
                        }
                    },
                    new GameMenuItem
                    {                        
                        Description = "Import Playtime from Nintendo json file",
                        MenuSection = $"{_menuSection}",
                        Icon = "BonusToolsUpdateIcon",
                        Action = a => {
                            new Nintendo(settings.Settings).ImportPlaytime(PlayniteApi);
                        }
                    },
                    new GameMenuItem
                    {                        
                        Description = "Import PS Price Paid (in Version field) from Sony export",
                        MenuSection = $"{_menuSection}",
                        Icon = "BonusToolsUpdateIcon",
                        Action = a => {
                             new PsPrices(settings.Settings).ImportPricePaid(PlayniteApi);
                        }
                    },
                    new GameMenuItem
                    {                        
                        Description = "Import PS Price Paid (in Version field) from Custom spreadsheet",
                        MenuSection = $"{_menuSection}",
                        Icon = "BonusToolsUpdateIcon",
                        Action = a => {
                             new CustomSpreadsheet(settings.Settings).ImportOwned(PlayniteApi);
                        }
                    },
                    new GameMenuItem
                    {                        
                        Description = "Import PS Plus status from PS Plus Master List as categories",
                        MenuSection = $"{_menuSection}",
                        Icon = "BonusToolsUpdateIcon",
                        Action = a => {
                             new PsMasterList(settings.Settings).ImportStatus(PlayniteApi);
                        }
                    },
                    new GameMenuItem
                    {                        
                        Description = "Import Yuzu Compatibility as categories",
                        MenuSection = $"{_menuSection}",
                        Icon = "BonusToolsUpdateIcon",
                        Action = a => {
                            Yuzu.ImportCompatibility(PlayniteApi);
                        }
                    },
                    new GameMenuItem
                    {                        
                        Description = "Import Ryujinx Compatibility as categories",
                        MenuSection = $"{_menuSection}",
                        Icon = "BonusToolsUpdateIcon",
                        Action = a => {
                            Ryujinx.ImportCompatibility(PlayniteApi);
                        }
                    }


                };
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override System.Windows.Controls.UserControl GetSettingsView(bool firstRunSettings)
        {
            return new BonusSettingsView();
        }


    }

}