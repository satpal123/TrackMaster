using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using TrackMaster.Helper;
using TrackMaster.Hubs;
using TrackMaster.Models;

namespace TrackMaster.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHubContext<TrackistHub> _tracklisthubContext;
        private readonly DataFields _dataFields;

        public HomeController(IHubContext<TrackistHub> synchub, DataFields dataFields)
        {
            _tracklisthubContext = synchub;
            _dataFields = dataFields;
        }
        public IActionResult Index()
        {
            ViewBag.ControllerFound = _dataFields.ControllerFound;
            ViewBag.TwitchBotManuallyStopped = _dataFields.TwitchBotManuallyStopped;
            ViewBag.DiscordBotManuallyStopped = _dataFields.DiscordBotManuallyStopped;
            ViewBag.IsConnectedTwitch = _dataFields.IsConnectedTwitch;
            ViewBag.IsConnectedDiscord = _dataFields.IsConnectedDiscord;

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
