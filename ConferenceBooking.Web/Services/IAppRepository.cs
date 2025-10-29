using ConferenceBooking.Web.Models;

namespace ConferenceBooking.Web.Services;

/// <summary>
/// In-memory repository contract for rooms and users.
/// </summary>
public interface IAppRepository
{
    // Rooms
    IReadOnlyList<Room> GetRooms();
    Room AddRoom(string name, int capacity);
    bool DeleteRoom(Guid id);

    // Users
    IReadOnlyList<AppUser> GetUsers();
    AppUser AddOrGetUser(string login, string role);
    AppUser? FindUser(string login);

    // Bookings
    IReadOnlyList<Booking> GetBookingsForDay(DateOnly day, TimeZoneInfo tz);
    (bool ok, string? error, Booking? booking) TryCreateBooking(Guid roomId, DateTime startLocal, DateTime endLocal, string title, string createdByLogin, TimeZoneInfo tz);
}
