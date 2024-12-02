using Microsoft.AspNetCore.Mvc;

namespace minstances.Controllers
{
    public class TextSearchController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
