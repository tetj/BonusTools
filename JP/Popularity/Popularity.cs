using Playnite.SDK.Models;
using Playnite.SDK;
using System.Collections.Generic;
using System.Linq;
using BonusTools;
using System;
using BonusTools.JP;

namespace BonusTools
{
    internal class Popularity
    {
        // Update using Steam, fallback to SensCritique and IGDB
        // TODO : I update the community score when using IGDB but not when using Steam .. I need a more consistent approach
        public static void UpdatePlayCount(List<Game> games, IPlayniteAPI PlayniteApi, string steamApiLanguage, string dataPath, BonusSettings settings)
        {
            var progressTitle = ResourceProvider.GetString("LOCReview_Viewer_DialogDataUpdateProgressMessage");
            var progressOptions = new GlobalProgressOptions(progressTitle, true);
            progressOptions.IsIndeterminate = false;
            PlayniteApi.Dialogs.ActivateGlobalProgress(async (a) =>
            {
                a.ProgressMaxValue = games.Count() + 1;

                using (PlayniteApi.Database.BufferedUpdate()) { 
                    foreach (Game game in games)
                    {
                        if (a.CancelToken.IsCancellationRequested)
                        {
                            break;
                        }
                        a.CurrentProgressValue++;
                        a.Text = $"{progressTitle}\n\n{a.CurrentProgressValue}/{a.ProgressMaxValue - 1}\n{game.Name}";

                        try { 
                            Steam.Update(game, PlayniteApi, steamApiLanguage, dataPath);                            
                            if (game.PlayCount == 0)
                            {
                                // TODO : probably not the most efficient way to update only the PlayCount at this stage
                                // if we updated all fields at once, we would save time but it would be less flexible
                                BonusSettings tempSettings = new BonusSettings();
                                tempSettings.UpdatePlayCountFromSensCritique = true;
                                tempSettings.UpdateUserScoreFromSensCritique = false;
                                tempSettings.AddLinkToSensCritique = false;
                                new SensCritiqueV2(tempSettings).UpdateGameRating(PlayniteApi, game);                                                         
                            }
                            if(game.PlayCount == 0)
                            {
                                IGDB.UpdateScores(game, PlayniteApi, settings).Wait();                                
                            }
                        }
                        catch (Exception ex)
                        {
                            PlayniteApi.Dialogs.ShowMessage("Errror : " + ex.Message, "BonusTools");
                        }
                    }
                }
            }, progressOptions);            
            PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCReview_Viewer_DialogResultsDataRefreshFinishedMessage"), "Popularity updater");
        }
    }
}
