using Microsoft.AspNetCore.Mvc;

namespace ManagerApp.Controllers.Authentication
{
    public class AuthController : Controller
    {
        [HttpGet]
        public IActionResult Auth()
        {
            return View();
        }
    }
}
