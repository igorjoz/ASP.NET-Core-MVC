using ConferenceBooking.Web.Filters;
using ConferenceBooking.Web.Models;
using ConferenceBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceBooking.Web.Controllers.Api;

[ApiController]
[Route("api/booking")] 
[LoggedInOnly]
public class BookingApiController : ControllerBase
{
    private readonly IAppRepository _repo;

    public BookingApiController(IAppRepository repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Returns rooms and bookings for the requested local date.
    /// </summary>
    [HttpGet("getForDay")]
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

    public record CreateRequest(Guid RoomId, DateTime StartLocal, DateTime EndLocal, string Title);

    /// <summary>
    /// Creates a booking for a given room and local time range.
    /// </summary>
    [HttpPost("create")]
    public IActionResult Create([FromBody] CreateRequest req)
    {
        var login = HttpContext.Session.GetString("UserLogin")!;
        var tz = TimeZoneInfo.Local;
        var (ok, error, booking) = _repo.TryCreateBooking(req.RoomId, req.StartLocal, req.EndLocal, req.Title, login, tz);
        if (!ok)
        {
            return BadRequest(new { error });
        }
        return Ok(new { id = booking!.Id });
    }
}
