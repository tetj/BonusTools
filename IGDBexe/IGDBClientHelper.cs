using IGDB;
using IGDB.Models;
using System.Linq;
using System.Threading.Tasks;

namespace BonusTools
{
    public class IGDBClientHelper
    {
        // TODO : ask users to provide their own API key ?
        // see https://api-docs.igdb.com/#getting-started

        public const string PopularityPrimitives = "popularity_primitives";
        public const string PopularityTypes = "popularity_types";

        public static string FetchGameRatings(string gameName, string CLIENT_ID, string CLIENT_SECRET)
        {
            var api = new IGDBClient(CLIENT_ID, CLIENT_SECRET);     
            var result = GetRatingCount(gameName, api).Result;
            return result;
        }

        private static async Task<string> GetRatingCount(string gameName, IGDBClient api)
        {
            Game? igdbGame = await SearchByName(gameName, api);
            var data = $"{igdbGame?.Rating}|{igdbGame?.TotalRatingCount}";
            return data;
        }

        public static async Task<Game?> SearchByName(string gameName, IGDBClient api)
        {
            var igdbGames = await api.QueryAsync<Game>(
                IGDBClient.Endpoints.Games,
                $"fields name, id, rating,total_rating_count; where name ~ *\"{gameName}\"*; sort total_rating_count desc;"
            );

            var igdbGame = igdbGames.FirstOrDefault();
            //Console.WriteLine($"Found Game: {igdbGame?.Name}");        
            return igdbGame;
        }
    }
}