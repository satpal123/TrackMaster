using Microsoft.AspNetCore.Mvc;
using TrackMaster.Helper;
using TrackMaster.Models;
using TrackMaster.Services.TwitchServices;

namespace TrackMaster.Controllers
{
    public class SettingsController : Controller
    {
        private Root root;
        private readonly DataFields _dataFields;

        public SettingsController(DataFields dataFields)
        {            
            _dataFields = dataFields;
        }
        public IActionResult Index()
        {
            TwitchCredentialsModel twitchCredentialsModel = new();
            SettingsHelper settingsHelper = new(_dataFields);
            root = settingsHelper.GetTwitchCredentials(_dataFields.Appfullpath);

            ViewBag.TwitchCredentials = root;

            if (root.TwitchCredentials != null)
            {
                twitchCredentialsModel.Username = root.TwitchCredentials.Username;
                twitchCredentialsModel.Password = root.TwitchCredentials.Password;
                twitchCredentialsModel.Channel = root.TwitchCredentials.Channel;
            }            

            return View(twitchCredentialsModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveSettings(TwitchCredentialsModel twitchCredentialsModel)
        {
            if (ModelState.IsValid)
            {
                SettingsHelper settingsHelper = new(_dataFields);
                settingsHelper.SetTwitchCredentials(twitchCredentialsModel);
                _dataFields.IsConnected = false;
            }

            return Json(new { title = "Notification", message = "Settings saved!", result = twitchCredentialsModel });
        }
    }
}
