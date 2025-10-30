using Microsoft.AspNetCore.Mvc;

using ConferenceBooking.Web.Filters;
using ConferenceBooking.Web.Services;
using ConferenceBooking.Web.Models;

namespace ConferenceBooking.Web.Controllers;

public class BookingController : Controller
{
    private readonly IAppRepository _repo;

    public BookingController(IAppRepository repo)
    {
        _repo = repo;
    }
    
    private static string EscapeIcsText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n");
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

    public class MyBookingViewModel
    {
        public Guid Id { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime StartLocal { get; set; }
        public DateTime EndLocal { get; set; }
    }

    /// <summary>
    /// Shows upcoming bookings for the logged-in user with the ability to cancel.
    /// Route: GET /Booking/MyBookings
    /// </summary>
    [HttpGet]
    [LoggedInOnly]
    public IActionResult MyBookings()
    {
        var login = HttpContext.Session.GetString("UserLogin")!;
        var tz = TimeZoneInfo.Local;
        var rooms = _repo.GetRooms().ToDictionary(r => r.Id, r => r.Name);
        var upcoming = _repo.GetUpcomingBookingsForUser(login);

        var model = upcoming
            .Select(b => new MyBookingViewModel
            {
                Id = b.Id,
                RoomName = rooms.TryGetValue(b.RoomId, out var rn) ? rn : b.RoomId.ToString(),
                Title = b.Title,
                StartLocal = TimeZoneInfo.ConvertTimeFromUtc(b.StartUtc, tz),
                EndLocal = TimeZoneInfo.ConvertTimeFromUtc(b.EndUtc, tz)
            })
            .OrderBy(m => m.StartLocal)
            .ToList();

        return View(model);
    }

    /// <summary>
    /// Exports the logged-in user's upcoming bookings as an iCalendar (.ics) file.
    /// Route: GET /Booking/ExportMyBookings
    /// </summary>
    [HttpGet]
    [LoggedInOnly]
    public IActionResult ExportMyBookings()
    {
        var login = HttpContext.Session.GetString("UserLogin")!;
        var rooms = _repo.GetRooms().ToDictionary(r => r.Id, r => r.Name);
        var upcoming = _repo.GetUpcomingBookingsForUser(login);

        // Build ICS in UTC with CRLF line endings.
        var nowUtc = DateTime.UtcNow;
        string F(DateTime dt) => dt.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");

        var sb = new System.Text.StringBuilder();
        void L(string line) => sb.Append(line).Append("\r\n");

        L("BEGIN:VCALENDAR");
        L("PRODID:-//ConferenceBooking//Export//EN");
        L("VERSION:2.0");
        L($"X-WR-CALNAME:{EscapeIcsText($"Bookings for {login}")}");
        L("CALSCALE:GREGORIAN");
        L("METHOD:PUBLISH");

        foreach (var b in upcoming)
        {
            var summary = string.IsNullOrWhiteSpace(b.Title) ? "Meeting" : b.Title;
            var roomName = rooms.TryGetValue(b.RoomId, out var rn) ? rn : b.RoomId.ToString();
            var uid = $"{b.Id}@conferencebooking.local";

            L("BEGIN:VEVENT");
            L($"UID:{uid}");
            L($"DTSTAMP:{F(nowUtc)}");
            L($"DTSTART:{F(b.StartUtc)}");
            L($"DTEND:{F(b.EndUtc)}");
            L($"SUMMARY:{EscapeIcsText(summary)}");
            L($"LOCATION:{EscapeIcsText(roomName)}");
            L($"ORGANIZER;CN={EscapeIcsText(login)}:MAILTO:noreply@example.com");
            L("END:VEVENT");
        }

        L("END:VCALENDAR");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"my-bookings-{login}.ics";
        return File(bytes, "text/calendar; charset=utf-8", fileName);
    }

    /// <summary>
    /// Cancels a booking owned by the logged-in user.
    /// Route: POST /Booking/Cancel
    /// </summary>
    [HttpPost]
    [LoggedInOnly]
    public IActionResult Cancel([FromForm] Guid id)
    {
        var login = HttpContext.Session.GetString("UserLogin")!;
        var (ok, error) = _repo.TryCancelBooking(id, login);
        if (!ok)
        {
            TempData["Error"] = error ?? "Failed to cancel the booking.";
        }
        else
        {
            TempData["Message"] = "Booking canceled.";
        }
        return RedirectToAction(nameof(MyBookings));
    }
}
