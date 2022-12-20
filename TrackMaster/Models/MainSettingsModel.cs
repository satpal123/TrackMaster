using System.Text.Json.Serialization;

namespace TrackMaster.Models
{
    public class MainSettingsModel
    {
        [JsonPropertyName("TwitchCredentials")]
        public TwitchCredentialsModel TwitchCredentials { get; set; }

        [JsonPropertyName("OverlaySettings")]
        public OverlaySettingsModel OverlaySettings { get; set; }
    }
}
