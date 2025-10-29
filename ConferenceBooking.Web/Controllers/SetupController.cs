using ConferenceBooking.Web.Filters;
using ConferenceBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceBooking.Web.Controllers;

public class SetupController : Controller
{
    private readonly IAppRepository _repo;

    public SetupController(IAppRepository repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Seeds a few sample users and rooms. Admin only.
    /// </summary>
    [HttpGet("/Init")]
    [AdminOnly]
    public IActionResult Init()
    {
        // Users
        _repo.AddOrGetUser("admin", "Admin");
        _repo.AddOrGetUser("alice", "User");
        _repo.AddOrGetUser("bob", "User");

        // Rooms (idempotent by checking existing names)
        var existing = _repo.GetRooms();
        var names = existing.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!names.Contains("Boardroom")) _repo.AddRoom("Boardroom", 10);
        if (!names.Contains("Huddle")) _repo.AddRoom("Huddle", 4);
        if (!names.Contains("Lecture Hall")) _repo.AddRoom("Lecture Hall", 30);
        if (!names.Contains("Studio")) _repo.AddRoom("Studio", 8);

        return RedirectToAction("Manage", "Room");
    }
}
