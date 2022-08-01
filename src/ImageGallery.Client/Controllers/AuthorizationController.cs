using Microsoft.AspNetCore.Mvc;

namespace ImageGallery.Client.Controllers
{

    public class Authorization : Controller
    {
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}