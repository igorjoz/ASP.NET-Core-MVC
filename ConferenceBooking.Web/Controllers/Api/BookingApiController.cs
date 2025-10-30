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

    public class CreateRequest : System.ComponentModel.DataAnnotations.IValidatableObject
    {
        [System.ComponentModel.DataAnnotations.Required]
        public Guid RoomId { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public DateTime StartLocal { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public DateTime EndLocal { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(200, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(System.ComponentModel.DataAnnotations.ValidationContext validationContext)
        {
            if (StartLocal >= EndLocal)
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult(
                    "Start must be before end.", new[] { nameof(StartLocal), nameof(EndLocal) });
                yield break;
            }

            var duration = EndLocal - StartLocal;
            if (duration < TimeSpan.FromMinutes(15))
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult(
                    "Minimum booking duration is 15 minutes.", new[] { nameof(StartLocal), nameof(EndLocal) });
            }
            if (duration > TimeSpan.FromHours(3))
            {
                yield return new System.ComponentModel.DataAnnotations.ValidationResult(
                    "Maximum booking duration is 3 hours.", new[] { nameof(StartLocal), nameof(EndLocal) });
            }
        }
    }

    /// <summary>
    /// Creates a booking for a given room and local time range.
    /// </summary>
    [HttpPost("create")]
    public IActionResult Create([FromBody] CreateRequest req)
    {
        if (!ModelState.IsValid)
        {
            // Return the first validation message (UI displays a single error line)
            var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                             ?? "Validation error.";
            return BadRequest(new { error = firstError });
        }
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
