using Microsoft.AspNetCore.Mvc;

namespace ConferenceBooking.Web.Controllers;

public class BookingController : Controller
{
    /// <summary>
    /// Main calendar view placeholder.
    /// </summary>
    [HttpGet]
    public IActionResult Calendar()
    {
        return View();
    }
}
