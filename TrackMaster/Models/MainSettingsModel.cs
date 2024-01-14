using System.Text.Json.Serialization;

namespace TrackMaster.Models
{
    public class MainSettingsModel
    {
        [JsonPropertyName("TwitchCredentials")]
        public TwitchCredentialsModel TwitchCredentials { get; set; }

        [JsonPropertyName("DiscordCredentials")]
        public DiscordCredentialsModel DiscordCredentials { get; set; }

        [JsonPropertyName("OverlaySettings")]
        public OverlaySettingsModel OverlaySettings { get; set; }

        [JsonPropertyName("OtherSettings")]
        public OtherSettingsModel OtherSettings { get; set; }

    }
}
