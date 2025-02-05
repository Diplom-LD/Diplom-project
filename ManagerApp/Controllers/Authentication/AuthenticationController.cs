using Microsoft.AspNetCore.Mvc;

namespace ManagerApp.Controllers.Authentication
{
    public class AuthenticationController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
    }
}
