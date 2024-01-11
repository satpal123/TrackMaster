using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TrackMaster.Models
{
    public class DiscordCredentialsModel
    {
        [DisplayName("Channel ID")]
        [Required(ErrorMessage = "Enter Discord ChannelId")]
        [JsonPropertyName("ChannelId")]
        public ulong ChannelId { get; set; }

        [DisplayName("Discord Token")]
        [Required(ErrorMessage = "Enter the Discord Token")]
        [JsonPropertyName("DiscordToken")]
        public string DiscordToken { get; set; }
    }
}
