using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Mvc;

namespace TrackMaster.Controllers
{
    public class ShellController : Controller
    {
        public IActionResult Index()
        {
            if (HybridSupport.IsElectronActive)
            {
                Electron.IpcMain.On("open-file-manager", async (args) =>
                {
                    string path = await Electron.App.GetPathAsync(PathName.UserData);
                    await Electron.Shell.ShowItemInFolderAsync(path);

                });  
            }

            return View();
        }
    }
}
