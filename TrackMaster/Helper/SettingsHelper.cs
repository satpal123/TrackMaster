using System.IO;
using System.Text.Json;
using TrackMaster.Models;

namespace TrackMaster.Helper
{
    public class SettingsHelper
    {        
        private string filecontent;

        private readonly DataFields _dataFields;
        public SettingsHelper(DataFields dataFields)
        {
            _dataFields = dataFields;
        }

        public MainSettingsModel SetMainSettings(TwitchCredentialsModel TwitchCredentials, OverlaySettingsModel OverlaySettings, 
            DiscordCredentialsModel DiscordCredentials)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            if (TwitchCredentials != null)
            {
                TwitchCredentials = new TwitchCredentialsModel
                {
                    Username = TwitchCredentials.Username,
                    Password = TwitchCredentials.Password,
                    Channel = TwitchCredentials.Channel
                };
            }           

            if (DiscordCredentials != null )
            {
                DiscordCredentials = new DiscordCredentialsModel
                {
                    ChannelId = DiscordCredentials.ChannelId,
                    DiscordToken = DiscordCredentials.DiscordToken
                };
            }

            if (OverlaySettings!= null)
            {
                OverlaySettings = new OverlaySettingsModel
                {
                    DisplayAlbumArt = OverlaySettings.DisplayAlbumArt,
                };
            }

            MainSettingsModel mainSettingsModel = new()
            {
                TwitchCredentials = TwitchCredentials,
                OverlaySettings = OverlaySettings,
                DiscordCredentials = DiscordCredentials
            };

            filecontent = JsonSerializer.Serialize(mainSettingsModel, options);
            File.WriteAllText(_dataFields.Appfullpath, filecontent);

            return mainSettingsModel;
        }       

        public MainSettingsModel GetSettings(string settingsPath)
        {
            MainSettingsModel mainSettings  = new();

            var filecheck = File.Exists(settingsPath);
            
            if (filecheck)
            {
                if (new FileInfo(settingsPath).Length != 0)
                {
                    filecontent = File.ReadAllText(settingsPath);

                    mainSettings = JsonSerializer.Deserialize<MainSettingsModel>(filecontent);
                }
            } 
            return mainSettings;
        }
    }
}
