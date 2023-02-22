using System.IO;
using System.Text.Json;
using TrackMaster.Models;

namespace TrackMaster.Helper
{
    public class RestoreTrackMetadata
    {
        private string filecontent;

        private readonly DataFields _dataFields;
        public RestoreTrackMetadata(DataFields dataFields)
        {
            _dataFields = dataFields;
        }

        public void SetMainSettings(DataFields dataFields)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            }; 

            RestoreTrackMetadataModel restoreTrackMetadataModel = new()
            {
                PlayersTrackMetaDataModel = dataFields
            };

            filecontent = JsonSerializer.Serialize(restoreTrackMetadataModel, options);
            File.WriteAllText(_dataFields.RestoreTrackspath, filecontent);
        }

        public RestoreTrackMetadataModel GetSettings(string settingsPath)
        {
            RestoreTrackMetadataModel restoreTrackMetadata = new();

            var filecheck = File.Exists(settingsPath);

            if (filecheck)
            {
                if (new FileInfo(settingsPath).Length != 0)
                {
                    filecontent = File.ReadAllText(settingsPath);

                    restoreTrackMetadata = JsonSerializer.Deserialize<RestoreTrackMetadataModel>(filecontent);
                }
            }
            else
            {
                using StreamWriter w = File.CreateText(settingsPath);
            }
            return restoreTrackMetadata;
        }
    }
}
