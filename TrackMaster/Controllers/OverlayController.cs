using Microsoft.AspNetCore.Mvc;

namespace TrackMaster.Controllers
{
    public class OverlayController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult NowPlaying()
        {
            return View();
        }
    }
}
