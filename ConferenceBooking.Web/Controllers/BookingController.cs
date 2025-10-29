using Microsoft.AspNetCore.Mvc;

using ConferenceBooking.Web.Filters;

namespace ConferenceBooking.Web.Controllers;

public class BookingController : Controller
{
    /// <summary>
    /// Main calendar view placeholder.
    /// </summary>
    [HttpGet]
    [LoggedInOnly]
    public IActionResult Calendar()
    {
        return View();
    }
}
