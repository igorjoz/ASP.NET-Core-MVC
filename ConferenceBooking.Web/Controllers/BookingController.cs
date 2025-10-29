using Microsoft.AspNetCore.Mvc;

using ConferenceBooking.Web.Filters;
using ConferenceBooking.Web.Services;

namespace ConferenceBooking.Web.Controllers;

public class BookingController : Controller
{
    private readonly IAppRepository _repo;

    public BookingController(IAppRepository repo)
    {
        _repo = repo;
    }
    /// <summary>
    /// Main calendar view placeholder.
    /// </summary>
    [HttpGet]
    [LoggedInOnly]
    public IActionResult Calendar()
    {
        return View();
    }

    /// <summary>
    /// Returns rooms and bookings for a given local date as JSON.
    /// Route: GET /Booking/GetForDay?date=YYYY-MM-DD
    /// </summary>
    [HttpGet]
    [Produces("application/json")]
    [LoggedInOnly]
    public IActionResult GetForDay([FromQuery] DateOnly date)
    {
        var tz = TimeZoneInfo.Local;
        var rooms = _repo.GetRooms();
        var bookings = _repo.GetBookingsForDay(date, tz);
        return Ok(new
        {
            rooms = rooms.Select(r => new { r.Id, r.Name, r.Capacity }),
            bookings = bookings.Select(b => new
            {
                b.Id,
                b.RoomId,
                b.Title,
                startLocal = TimeZoneInfo.ConvertTimeFromUtc(b.StartUtc, tz),
                endLocal = TimeZoneInfo.ConvertTimeFromUtc(b.EndUtc, tz),
                b.CreatedByLogin
            })
        });
    }
}
