using Playnite.SDK.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BonusTools.JP;
using Playnite.SDK.Plugins;

namespace BonusTools
{
    internal class Popularity
    {
        // Update using Steam, fallback to IGDB
        // TODO : IGDB relies on an external program, I need to remove that
        // TODO : also, I update the community score when using IGDB but not when using Steam .. I need a more consistent approach
        public static void Update(List<Game> games, IPlayniteAPI PlayniteApi, string steamApiLanguage, string dataPath)
        {
            var progressTitle = ResourceProvider.GetString("LOCReview_Viewer_DialogDataUpdateProgressMessage");
            var progressOptions = new GlobalProgressOptions(progressTitle, true);
            progressOptions.IsIndeterminate = false;
            PlayniteApi.Dialogs.ActivateGlobalProgress(async (a) =>
            {
                a.ProgressMaxValue = games.Count() + 1;

                foreach (Game game in games)
                {
                    if (a.CancelToken.IsCancellationRequested)
                    {
                        break;
                    }
                    a.CurrentProgressValue++;
                    a.Text = $"{progressTitle}\n\n{a.CurrentProgressValue}/{a.ProgressMaxValue - 1}\n{game.Name}";

                    Steam.Update(game, PlayniteApi, steamApiLanguage, dataPath);
                    if (game.UserScore == null || game.UserScore == 0)
                    {
                        await IGDB.UpdateScores(game, PlayniteApi);
                    }
                }
            }, progressOptions);

            PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCReview_Viewer_DialogResultsDataRefreshFinishedMessage"), "Popularity updater");
        }
    }
}
