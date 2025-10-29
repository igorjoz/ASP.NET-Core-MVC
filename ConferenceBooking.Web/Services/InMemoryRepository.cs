using ConferenceBooking.Web.Models;

namespace ConferenceBooking.Web.Services;

/// <summary>
/// Thread-safe in-memory repository. For simplicity, we use a single lock for rooms and users.
/// </summary>
public class InMemoryRepository : IAppRepository
{
    private readonly object _roomLock = new();
    private readonly object _userLock = new();
    private readonly List<Room> _rooms = new();
    private readonly List<AppUser> _users = new();

    // Rooms
    public IReadOnlyList<Room> GetRooms()
    {
        lock (_roomLock)
        {
            // Return a copy to protect internal state.
            return _rooms.Select(r => new Room
            {
                Id = r.Id,
                Name = r.Name,
                Capacity = r.Capacity,
                CreatedAtUtc = r.CreatedAtUtc
            }).ToList();
        }
    }

    public Room AddRoom(string name, int capacity)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");

        lock (_roomLock)
        {
            var room = new Room { Name = name.Trim(), Capacity = capacity };
            _rooms.Add(room);
            return room;
        }
    }

    public bool DeleteRoom(Guid id)
    {
        lock (_roomLock)
        {
            var idx = _rooms.FindIndex(r => r.Id == id);
            if (idx >= 0)
            {
                _rooms.RemoveAt(idx);
                return true;
            }
            return false;
        }
    }

    // Users
    public IReadOnlyList<AppUser> GetUsers()
    {
        lock (_userLock)
        {
            return _users.Select(u => new AppUser { Login = u.Login, Role = u.Role }).ToList();
        }
    }

    public AppUser AddOrGetUser(string login, string role)
    {
        if (string.IsNullOrWhiteSpace(login)) throw new ArgumentException("Login cannot be empty.", nameof(login));
        if (string.IsNullOrWhiteSpace(role)) throw new ArgumentException("Role cannot be empty.", nameof(role));

        lock (_userLock)
        {
            var existing = _users.FirstOrDefault(u => string.Equals(u.Login, login, StringComparison.OrdinalIgnoreCase));
            if (existing != null) return existing;
            var user = new AppUser { Login = login.Trim(), Role = role };
            _users.Add(user);
            return user;
        }
    }

    public AppUser? FindUser(string login)
    {
        if (string.IsNullOrWhiteSpace(login)) return null;
        lock (_userLock)
        {
            return _users.FirstOrDefault(u => string.Equals(u.Login, login, StringComparison.OrdinalIgnoreCase));
        }
    }
}
