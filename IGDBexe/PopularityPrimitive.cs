using IGDB;
using IGDB.Models;
using System;
using System.Text.Json.Serialization;

namespace BonusTools
{
	public class CustomGame : Game
    {

        [JsonPropertyName("popularity_type")]
        public int PopularityType { get; set; }

	}

    public class PopularityPrimitive : ITimestamps //, IHasChecksum
    {
        public DateTimeOffset? CalculatedAt { get; set; }
        /*
		 * Even though the IGDB API documentation states that the checksum field
		 * is available for this model, the API does not return it. This is why
		 * the Checksum property is commented out for now.
		 */
        //public string Checksum { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public long? GameId { get; set; }
        public PopularitySource? PopularitySource { get; set; }
        public IdentityOrValue<PopularityType> PopularityType { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public decimal? Value { get; set; }
    }
}
