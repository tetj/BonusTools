using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Collections.Generic;

namespace BonusTools
{
    public class GameExport
    {
        [JsonPropertyName("exportString")]
        public string ExportString { get; set; }

        [JsonPropertyName("exportTimestamp")]
        public long ExportTimestamp { get; set; }

        [JsonPropertyName("exportVersion")]
        public string ExportVersion { get; set; }

        [JsonPropertyName("users")]
        public List<User> Users { get; set; }
    }

    public class User
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("titles")]
        public List<Title> Titles { get; set; }
    }

    public class Title
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("summary")]
        public Summary Summary { get; set; }

        [JsonIgnore]
        public string FormattedFirstPlayed => FormatTimestamp(Summary.FirstPlayed);

        [JsonIgnore]
        public string FormattedLastPlayed => FormatTimestamp(Summary.LastPlayed);

        // Helper method to format Unix timestamp
        private string FormatTimestamp(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp)
                .LocalDateTime
                .ToString("MMMM dd, yyyy HH:mm:ss");
        }
    }

    public class Summary
    {
        [JsonPropertyName("firstPlayed")]
        public long FirstPlayed { get; set; }

        [JsonPropertyName("lastPlayed")]
        public long LastPlayed { get; set; }

        [JsonPropertyName("launches")]
        public int Launches { get; set; }

        [JsonPropertyName("playtime")]
        public int Playtime { get; set; }
    }
}