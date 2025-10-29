using Microsoft.AspNetCore.Mvc;

namespace ConferenceBooking.Web.Controllers;

public class HomeController : Controller
{
    /// <summary>
    /// Public landing page with quick login links.
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
