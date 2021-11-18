using Microsoft.AspNetCore.Mvc;
using TrackMaster.Helper;
using TrackMaster.Models;
using TrackMaster.Services.TwitchServices;

namespace TrackMaster.Controllers
{
    public class SettingsController : Controller
    {
        private Root root;
        public IActionResult Index()
        {
            TwitchCredentialsModel twitchCredentialsModel = new TwitchCredentialsModel();
            SettingsHelper settingsHelper = new SettingsHelper();
            root = settingsHelper.GetTwitchCredentials(TwitchBot.appfullpath);

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
                SettingsHelper settingsHelper = new SettingsHelper();
                settingsHelper.SetTwitchCredentials(twitchCredentialsModel);
                TwitchBot.IsConnected = false;
            }

            return Json(new { title = "Notification", message = "Settings saved!", result = twitchCredentialsModel });
        }
    }
}
