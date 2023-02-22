using System.Text.Json.Serialization;
using TrackMaster.Helper;

namespace TrackMaster.Models
{
    public class RestoreTrackMetadataModel
    {
        [JsonPropertyName("DataFields")]
        public DataFields PlayersTrackMetaDataModel { get; set; }
    }
}
