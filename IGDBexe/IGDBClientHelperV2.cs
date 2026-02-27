using IGDB;
using IGDB.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Playnite.SDK;

namespace BonusTools
{
    public class IGDBClientHelperV2
    {        
        // see https://api-docs.igdb.com/#getting-started
        private static readonly Playnite.SDK.ILogger Logger = LogManager.GetLogger();
        private string igdbAccessToken;
        private BonusSettings settings;

        public const string PopularityPrimitives = "popularity_primitives";
        public const string PopularityTypes = "popularity_types";

        public IGDBClientHelperV2(BonusSettings setttings)
        {
            this.settings = setttings;
            igdbAccessToken = GetIGDBAccessToken();
        }

        private string GetIGDBAccessToken()
        {
            try
            {
                using (var client = new WebClient())
                {
                    var values = new System.Collections.Specialized.NameValueCollection
                    {
                        { "client_id", settings.ClientID },
                        { "client_secret", settings.ClientSecret },
                        { "grant_type", "client_credentials" }
                    };

                    var response = client.UploadValues("https://id.twitch.tv/oauth2/token", values);
                    var responseString = Encoding.Default.GetString(response);
                    var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

                    return json["access_token"];
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get IGDB access token.");
                //PlayniteApi.Dialogs.ShowErrorMessage($"Failed to get IGDB token: {ex.Message}", "IGDB Error");
                return null;
            }
        }
        public string SearchByName(string gameName)
        {
            if (string.IsNullOrEmpty(igdbAccessToken))
            {
                return null;
            }

            try
            {
                using (var client = new WebClient())
                {
                    client.Headers.Add("Client-ID", settings.ClientID);
                    client.Headers.Add("Authorization", $"Bearer {igdbAccessToken}");
                    client.Headers.Add("Accept", "application/json");

                    string query = $"fields name, id, rating,total_rating_count; where name ~ *\"{gameName}\"*; sort total_rating_count desc;";
                    var response = client.UploadString("https://api.igdb.com/v4/games", query);
                    var games = JsonConvert.DeserializeObject<List<dynamic>>(response);

                    if (games.Count > 0)
                    {
                        var data = $"{games[0].rating}|{games[0].total_rating_count}";
                        return data;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to fetch IGDB cover for {gameName}.");
            }

            return null;
        }


    }
}