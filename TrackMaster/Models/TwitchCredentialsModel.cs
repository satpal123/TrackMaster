using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TrackMaster.Models
{
    public class TwitchCredentialsModel
    {
        [DisplayName("Username")]
        [Required(ErrorMessage = "Enter Username")]
        [JsonPropertyName("Username")]
        public string Username { get; set; }

        [DisplayName("OAuth Password")]
        [Required(ErrorMessage = "Enter the oAuth password")]
        [JsonPropertyName("Password")]
        public string Password { get; set; }

        [DisplayName("Channel Name")]
        [Required(ErrorMessage = "Enter Channel name")]
        [JsonPropertyName("Channel")]
        public string Channel { get; set; }       

    }
}
