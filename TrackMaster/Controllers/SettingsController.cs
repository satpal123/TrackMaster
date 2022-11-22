using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using TrackMaster.Helper;
using TrackMaster.Hubs;
using TrackMaster.Models;
using TrackMaster.Services.TwitchServices;

namespace TrackMaster.Controllers
{
    public class SettingsController : Controller
    {
        private Root root;
        private readonly DataFields _dataFields;
        private readonly ITimerHostedService _hostedService;
        private readonly IHubContext<TrackistHub> _tracklisthubContext;

        public SettingsController(ITimerHostedService hostedService, DataFields dataFields, IHubContext<TrackistHub> synchub)
        {
            _hostedService = hostedService;
            _dataFields = dataFields;
            _tracklisthubContext = synchub;
        }
        public IActionResult Index()
        {
            TwitchCredentialsModel twitchCredentialsModel = new();
            SettingsHelper settingsHelper = new(_dataFields);
            root = settingsHelper.GetTwitchCredentials(_dataFields.Appfullpath);

            ViewBag.TwitchCredentials = root;
            ViewBag.BotManuallyStopped = _dataFields.BotManuallyStopped;

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

        [HttpPost]
        public async Task<IActionResult> StartStopBot()
        {
            if (_dataFields.BotManuallyStopped)
            {
                await _hostedService.StartAsync(new System.Threading.CancellationToken());
                _dataFields.BotManuallyStopped = false;
                _dataFields.IsConnected = false;
            }
            else
            {
                await _hostedService.StopAsync(new System.Threading.CancellationToken());
            }

            return Json(new { title = "Notification", message = "Settings saved!" });
        }
    }
}
