using ConferenceBooking.Web.Filters;
using ConferenceBooking.Web.Models;
using ConferenceBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceBooking.Web.Controllers;

[Route("Room")]
[AdminOnly]
public class RoomController : Controller
{
    private readonly IAppRepository _repo;

    public RoomController(IAppRepository repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Admin page to list, add and delete rooms.
    /// </summary>
    [HttpGet("Manage")]
    public IActionResult Manage()
    {
        var rooms = _repo.GetRooms();
        return View(rooms);
    }

    /// <summary>
    /// Adds a new room and redirects back to Manage.
    /// </summary>
    [HttpPost("Add")]
    [ValidateAntiForgeryToken]
    public IActionResult Add([FromForm] string name, [FromForm] int capacity)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError("name", "Name is required.");
        }
        if (capacity <= 0)
        {
            ModelState.AddModelError("capacity", "Capacity must be greater than zero.");
        }
        if (!ModelState.IsValid)
        {
            var rooms = _repo.GetRooms();
            return View("Manage", rooms);
        }

        _repo.AddRoom(name, capacity);
        return RedirectToAction("Manage");
    }

    /// <summary>
    /// Deletes a room by id and redirects back to Manage.
    /// </summary>
    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(Guid id)
    {
        _repo.DeleteRoom(id);
        return RedirectToAction("Manage");
    }
}
