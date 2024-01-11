using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using TrackMaster.Helper;
using TrackMaster.Hubs;
using TrackMaster.Models;
using ITimerHostedServiceDiscord = TrackMaster.Services.DiscordServices.ITimerHostedService;
using ITimerHostedServiceTwitch = TrackMaster.Services.TwitchServices.ITimerHostedService;

namespace TrackMaster.Controllers
{
    public class SettingsController : Controller
    {
        private MainSettingsModel MainSettingsModel;
        private readonly DataFields _dataFields;
        private readonly ITimerHostedServiceTwitch _hostedTwitchService;
        private readonly ITimerHostedServiceDiscord _hostedDiscordService;
        private readonly IHubContext<TrackistHub> _tracklisthubContext;

        public SettingsController(ITimerHostedServiceTwitch hostedTwitchService, ITimerHostedServiceDiscord hostedDiscordService, 
            DataFields dataFields, IHubContext<TrackistHub> synchub)
        {
            _hostedTwitchService = hostedTwitchService;
            _hostedDiscordService = hostedDiscordService;
            _dataFields = dataFields;
            _tracklisthubContext = synchub;
        }
        public IActionResult Index()
        {
            TwitchCredentialsModel twitchCredentialsModel = new();
            DiscordCredentialsModel discordCredentialsModel = new();
            SettingsHelper settingsHelper = new(_dataFields);
            MainSettingsModel = settingsHelper.GetSettings(_dataFields.Appfullpath);

            ViewBag.TwitchCredentials = MainSettingsModel.TwitchCredentials;
            ViewBag.TwitchBotManuallyStopped = _dataFields.TwitchBotManuallyStopped;

            ViewBag.DiscordCredentials = MainSettingsModel.DiscordCredentials;
            ViewBag.DiscordBotManuallyStopped = _dataFields.DiscordBotManuallyStopped;

            if (MainSettingsModel.TwitchCredentials != null)
            {
                twitchCredentialsModel.Username = MainSettingsModel.TwitchCredentials.Username;
                twitchCredentialsModel.Password = MainSettingsModel.TwitchCredentials.Password;
                twitchCredentialsModel.Channel = MainSettingsModel.TwitchCredentials.Channel;
            }

            if (MainSettingsModel.DiscordCredentials != null)
            {
                discordCredentialsModel.ChannelId = MainSettingsModel.DiscordCredentials.ChannelId;
                discordCredentialsModel.DiscordToken = MainSettingsModel.DiscordCredentials.DiscordToken;
            }

            MainSettingsModel mainSettingsModel = new MainSettingsModel
            {
                DiscordCredentials = discordCredentialsModel,
                TwitchCredentials= twitchCredentialsModel
            };

            return View(mainSettingsModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveTwitchSettings(MainSettingsModel mainSettingsModel)
        {
            if (ModelState.IsValid)
            {                
                SettingsHelper settingsHelper = new(_dataFields);

                MainSettingsModel = settingsHelper.GetSettings(_dataFields.Appfullpath);
                settingsHelper.SetMainSettings(mainSettingsModel.TwitchCredentials, MainSettingsModel.OverlaySettings, MainSettingsModel.DiscordCredentials);

                _dataFields.IsConnectedTwitch = false;
            }

            return Json(new { title = "Notification", message = "Twitch Settings saved!", result = mainSettingsModel });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveDiscordSettings(MainSettingsModel mainSettingsModel)
        {
            if (ModelState.IsValid)
            {
                SettingsHelper settingsHelper = new(_dataFields);

                MainSettingsModel = settingsHelper.GetSettings(_dataFields.Appfullpath);
                settingsHelper.SetMainSettings(MainSettingsModel.TwitchCredentials, MainSettingsModel.OverlaySettings, mainSettingsModel.DiscordCredentials);

                _dataFields.IsConnectedDiscord = false;
            }

            return Json(new { title = "Notification", message = "Discord Settings saved!", result = mainSettingsModel });
        }

        [HttpPost]
        public async Task<IActionResult> StartStopTwitchBot()
        {
            if (_dataFields.TwitchBotManuallyStopped)
            {
                await _hostedTwitchService.StartAsync(new System.Threading.CancellationToken());
                _dataFields.TwitchBotManuallyStopped = false;
                _dataFields.IsConnectedTwitch = false;
            }
            else
            {
                await _hostedTwitchService.StopAsync(new System.Threading.CancellationToken());
            }

            return Json(new { title = "Notification", message = "Settings saved!" });
        }

        [HttpPost]
        public async Task<IActionResult> StartStopDiscordBot()
        {
            if (_dataFields.DiscordBotManuallyStopped)
            {
                await _hostedDiscordService.StartAsync(new System.Threading.CancellationToken());
                _dataFields.DiscordBotManuallyStopped = false;
                _dataFields.IsConnectedDiscord = false;
            }
            else
            {
                await _hostedDiscordService.StopAsync(new System.Threading.CancellationToken());
            }

            return Json(new { title = "Notification", message = "Settings saved!" });
        }
    }
}
