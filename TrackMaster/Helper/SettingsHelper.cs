using System.Collections.Generic;
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

        public TwitchCredentialsModel SetMainSettings(TwitchCredentialsModel TwitchCredentials, OverlaySettingsModel OverlaySettings)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            TwitchCredentials = new TwitchCredentialsModel
            {
                Username = TwitchCredentials.Username,
                Password = TwitchCredentials.Password,
                Channel = TwitchCredentials.Channel
            };

            OverlaySettings = new OverlaySettingsModel
            {
                DisplayAlbumArt = OverlaySettings.DisplayAlbumArt,
            };

            MainSettingsModel mainSettingsModel = new()
            {
                TwitchCredentials = TwitchCredentials,
                OverlaySettings = OverlaySettings
            };

            filecontent = JsonSerializer.Serialize(mainSettingsModel, options);
            File.WriteAllText(_dataFields.Appfullpath, filecontent);

            return TwitchCredentials;
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
