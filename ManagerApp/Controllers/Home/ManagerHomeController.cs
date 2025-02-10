using Microsoft.AspNetCore.Mvc;

namespace ManagerApp.Controllers.Home
{
    public class ManagerHomeController : Controller
    {
        public IActionResult Home()
        {
            return View();
        }
    }
}
