using ElectronNET.API;
using ElectronNET.API.Entities;
using System;
using System.IO;
using System.Threading.Tasks;
using TrackMaster.Models;

namespace TrackMaster.Helper
{
    public static class BotSettingsHelper
    {
        public static async Task<MainSettingsModel> GetSettingsAsync(DataFields dataFields)
        {
            SettingsHelper settingsHelper = new(dataFields);

            if (HybridSupport.IsElectronActive)
            {
                string path = await Electron.App.GetPathAsync(PathName.UserData);
                dataFields.Appfullpath = Path.Combine(path, "Settings.json");
                return settingsHelper.GetSettings(dataFields.Appfullpath);
            }
            else
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                dataFields.Appfullpath = Path.Combine(appDataPath, "Electron", "Settings.json");
                return settingsHelper.GetSettings(dataFields.Appfullpath);
            }
        }
    }
}
