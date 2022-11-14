using System.IO;
using System.Text.Json;
using TrackMaster.Models;
using TrackMaster.Services.TwitchServices;

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

        public TwitchCredentialsModel SetTwitchCredentials(TwitchCredentialsModel TwitchCredentials)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var root = new
            {
                TwitchCredentials = new TwitchCredentialsModel
                {
                    Username = TwitchCredentials.Username,
                    Password = TwitchCredentials.Password,
                    Channel = TwitchCredentials.Channel
                }
            };

            filecontent = JsonSerializer.Serialize(root, options);
            File.WriteAllText(_dataFields.Appfullpath, filecontent);

            return TwitchCredentials;
        }

        public Root GetTwitchCredentials(string settingsPath)
        {
            Root root = new();

            var filecheck = File.Exists(settingsPath);
            
            if (filecheck)
            {
                if (new FileInfo(settingsPath).Length != 0)
                {
                    filecontent = File.ReadAllText(settingsPath);

                    root = JsonSerializer.Deserialize<Root>(filecontent);
                }
            } 
            return root;
        }
    }
}
