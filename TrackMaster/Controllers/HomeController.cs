using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using TrackMaster.Hubs;
using TrackMaster.Models;
using TrackMaster.Services.Sniffy;

namespace TrackMaster.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHubContext<TrackistHub> _tracklisthubContext;

        public HomeController(IHubContext<TrackistHub> synchub)
        {
            _tracklisthubContext = synchub;
        }
        public IActionResult Index()
        {
            ViewBag.ControllerFound = Sniffy.ControllerFound;

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
