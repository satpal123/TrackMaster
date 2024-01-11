using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TrackMaster.Helper;
using TrackMaster.Hubs;
using TrackMaster.Models;

namespace TrackMaster.Controllers
{
    public class OverlayController : Controller
    {
        private MainSettingsModel MainSettingsModel;
        private readonly DataFields _dataFields;
        private readonly IHubContext<TrackistHub> _tracklisthubContext;
        public OverlayController(DataFields dataFields, IHubContext<TrackistHub> synchub)
        {            
            _dataFields = dataFields;
            _tracklisthubContext = synchub;
        }
        public IActionResult Index()
        {            
            SettingsHelper settingsHelper = new(_dataFields);

            MainSettingsModel = settingsHelper.GetSettings(_dataFields.Appfullpath);            

            if (MainSettingsModel.OverlaySettings != null)
            {
                ViewBag.DisplayAlbumArt = _dataFields.ShowArtwork = MainSettingsModel.OverlaySettings.DisplayAlbumArt;
                _tracklisthubContext.Clients.All.SendAsync("Overlay", MainSettingsModel.OverlaySettings.DisplayAlbumArt);
            }

            return View();
        }

        [HttpPost]
        public IActionResult SaveSettings(OverlaySettingsModel overlaySettingsModel)
        {
            if (ModelState.IsValid)
            {
                SettingsHelper settingsHelper = new(_dataFields);

                if (_dataFields.Appfullpath != null)
                {
                    MainSettingsModel = settingsHelper.GetSettings(_dataFields.Appfullpath);
                    settingsHelper.SetMainSettings(MainSettingsModel.TwitchCredentials, overlaySettingsModel, MainSettingsModel.DiscordCredentials);
                    _dataFields.ShowArtwork = overlaySettingsModel.DisplayAlbumArt;

                    _tracklisthubContext.Clients.All.SendAsync("Overlay", overlaySettingsModel.DisplayAlbumArt);

                    _dataFields.IsConnectedTwitch = false;
                }
            }

            return Json(new { title = "Notification", message = "Settings saved!", result = overlaySettingsModel });
        }
        public IActionResult NowPlaying()
        {           
            SettingsHelper settingsHelper = new(_dataFields);

            MainSettingsModel = settingsHelper.GetSettings(_dataFields.Appfullpath);
            ViewBag.DisplayAlbumArt = _dataFields.ShowArtwork = MainSettingsModel.OverlaySettings.DisplayAlbumArt;
            _tracklisthubContext.Clients.All.SendAsync("Overlay", MainSettingsModel.OverlaySettings.DisplayAlbumArt);

            return View();
        }
    }
}
