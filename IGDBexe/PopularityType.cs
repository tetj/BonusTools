using IGDB.Models;
using System;
using System.Text.Json.Serialization;

namespace BonusTools
{
	public class PopularityType : ITimestamps, IHasChecksum
	{
		public string Checksum { get; set; }
		public DateTimeOffset? CreatedAt { get; set; }

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        public PopularitySource? PopularitySource { get; set; }
		public DateTimeOffset? UpdatedAt { get; set; }
	}
}
