using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static GraphQLServiceV2;

namespace BonusTools.JP
{
    public class SensCritiqueV2
    {
        private BonusSettings settings;
        private const double DEFAULT_MIN_MATCH_SCORE = 60;
        private const double PARTIAL_MATCH_THRESHOLD = 50;
        private const double RATING_COUNT_MULTIPLIER = 50;
        private readonly Playnite.SDK.ILogger logger;        

        public SensCritiqueV2(BonusSettings settings)
        {
            this.settings = settings;
            this.logger = LogManager.GetLogger();
        }

        public void UpdateAll(IPlayniteAPI PlayniteApi, List<Game> games)
        {
            var progressTitle = ResourceProvider.GetString("LOCReview_Viewer_DialogDataUpdateProgressMessage");
            var progressOptions = new GlobalProgressOptions(progressTitle, true);
            progressOptions.IsIndeterminate = false;
            PlayniteApi.Dialogs.ActivateGlobalProgress(async (a) =>
            {
                a.ProgressMaxValue = games.Count() + 1;

                foreach (var game in games)
                {
                    if (a.CancelToken.IsCancellationRequested)
                    {
                        break;
                    }
                    a.CurrentProgressValue++;
                    a.Text = $"{progressTitle}\n\n{a.CurrentProgressValue}/{a.ProgressMaxValue - 1}\n{game.Name}";

                    UpdateGameRating(PlayniteApi, game);
                }
            }, progressOptions);

            PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCReview_Viewer_DialogResultsDataRefreshFinishedMessage"), "SensCritiqueV2");
        }

        public void UpdateGameRating(IPlayniteAPI playniteApi, Game game)
        {
            try
            {
                bool success = false;
                if (settings.ReleaseYearMustMatch)
                {
                    var queryByYear = BuildQueryByYear(game);
                    success = TryUpdateRating(playniteApi, game, queryByYear).GetAwaiter().GetResult();
                }
                
                if (!success)
                {
                    var queryByPlatform = BuildQueryByPlatform(game);
                    TryUpdateRating(playniteApi, game, queryByPlatform).Wait();
                }
            }
            catch (Exception e)
            {
                logger.Info($"Error updating rating for game: {game.Name}. Error: {e.Message}");
            }
        }

        private async Task<bool> TryUpdateRating(IPlayniteAPI playniteApi, Game game, string query)
        {
            GraphQLServiceV2 v2 = new GraphQLServiceV2();
            var item = await v2.FindClosestMatchAsync(game.Name, query);            
            if (item == null)
            {
                logger.Info($"No result found: {game.Name}");
                return false;
            }

            var minMatchScore = GetMinimumMatchScore();

            if (item.bestMatchScore > minMatchScore)
            {
                UpdateGameRating(playniteApi, game, item);
                return true;
            }

            if (item.bestMatchScore > PARTIAL_MATCH_THRESHOLD)
            {
                logger.Info($"Found {PARTIAL_MATCH_THRESHOLD}-{minMatchScore}% match: {game.Name} | {item.Title} | {item.bestMatchScore}");
                return false;
            }

            logger.Info($"Match score too low for: {game.Name}");
            return false;
        }

        private double GetMinimumMatchScore()
        {
            return double.TryParse(settings.Min, out double minValue)
                ? minValue
                : DEFAULT_MIN_MATCH_SCORE;
        }

        private string BuildQueryByYear(Game game)
        {
            return $@"{{
            searchProductExplorer(
                query: ""{IGDB.RemoveEditions(game.Name)}""
                limit: 3
                sortBy: RELEVANCE
                infiniteScroll: false
                filters: [ 
                    {{identifier:""year"",termValues:""{game.ReleaseYear}""}}
                    {{identifier:""universe"",termValues:""game"" }}
                ]
            ) {{
                items {{
                    title
                    originalTitle
                    alternativeTitles
                    rating
                    stats {{
                        ratingCount
                    }}
                    url
                }}
            }}
        }}";
        }

        private string BuildQueryByPlatform(Game game)
        {
            var filters = new List<string>
            {
                @"{identifier:""universe"",termValues:""game"" }"
            };

            var platform = game.Platforms?.FirstOrDefault()?.Name
                ?.Replace(" (Windows)", "")
                .Replace("Sony ", "");

            if (settings.PlatformMustMatch && !string.IsNullOrEmpty(platform))
            {
                filters.Add($@"{{identifier:""game_systems"",termValues:""{platform}""}}");
            }

            return $@"{{
            searchProductExplorer(
                query: ""{IGDB.RemoveEditions(game.Name)}""
                limit: 3
                sortBy: RELEVANCE
                infiniteScroll: false
                filters: [ 
                    {string.Join(",\n                    ", filters)}
                ]
            ) {{
                items {{
                    title
                    originalTitle
                    alternativeTitles
                    rating
                    stats {{
                        ratingCount
                    }}
                    url
                }}
            }}
        }}";
        }

        private void UpdateGameRating(IPlayniteAPI PlayniteApi, Game game, ProductItem item)
        {
            using (PlayniteApi.Database.BufferedUpdate())
            {
                if(settings.UpdateUserScoreFromSensCritique)
                {
                    game.UserScore = (int?)(item.Rating * 10);
                    game.Modified = DateTime.Now;
                }

                if (settings.UpdatePlayCountFromSensCritique)
                {
                    if (game.PlayCount == 0)
                    {
                        if (item.Stats != null)
                        {
                            game.PlayCount = (ulong)(item.Stats.RatingCount * RATING_COUNT_MULTIPLIER);
                            game.Modified = DateTime.Now;
                            var logger = LogManager.GetLogger();
                            logger.Info("New rating count : " + game.Name);
                        }
                    }
                }

                if (settings.AddLinkToSensCritique)
                {
                    if (game.Links == null)
                    {
                        game.Links = new System.Collections.ObjectModel.ObservableCollection<Link>();
                    }
                    game.Links.Add(new Link("SensCritique", "https://www.senscritique.com" + item.Url));
                    game.Modified = DateTime.Now;
                }                
                PlayniteApi.Database.Games.Update(game);                
            }
        }

        internal void Count(IPlayniteAPI playniteApi, List<Game> games)
        {
            string count = games.Count().ToString();
            playniteApi.Dialogs.ShowMessage(count, "SensCritiqueV2");
        }
    }
}
