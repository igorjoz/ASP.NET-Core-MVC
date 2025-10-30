using ConferenceBooking.Web.Models;

namespace ConferenceBooking.Web.Services;

/// <summary>
/// Thread-safe in-memory repository. For simplicity, we use a single lock for rooms and users.
/// </summary>
public class InMemoryRepository : IAppRepository
{
    private readonly object _roomLock = new();
    private readonly object _userLock = new();
    private readonly Dictionary<Guid, object> _roomLocks = new();
    private readonly List<Room> _rooms = new();
    private readonly List<AppUser> _users = new();
    private readonly List<Booking> _bookings = new();

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

    // Bookings
    public IReadOnlyList<Booking> GetBookingsForDay(DateOnly day, TimeZoneInfo tz)
    {
        // Convert day boundary from local tz to UTC to filter bookings.
        var startLocal = day.ToDateTime(TimeOnly.MinValue);
        var endLocal = day.ToDateTime(TimeOnly.MaxValue);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, tz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, tz);

        lock (_bookings)
        {
            return _bookings
                .Where(b => b.EndUtc > startUtc && b.StartUtc < endUtc)
                .Select(b => new Booking
                {
                    Id = b.Id,
                    RoomId = b.RoomId,
                    Title = b.Title,
                    StartUtc = b.StartUtc,
                    EndUtc = b.EndUtc,
                    CreatedByLogin = b.CreatedByLogin,
                    CreatedAtUtc = b.CreatedAtUtc
                })
                .ToList();
        }
    }

    public (bool ok, string? error, Booking? booking) TryCreateBooking(Guid roomId, DateTime startLocal, DateTime endLocal, string title, string createdByLogin, TimeZoneInfo tz)
    {
        if (string.IsNullOrWhiteSpace(title)) return (false, "Title is required.", null);
        if (startLocal >= endLocal) return (false, "Start must be before end.", null);

        // Normalize incoming values that might be Utc (because JS Date -> ISO string with Z)
        DateTime NormalizeToLocalWall(DateTime dt)
        {
            return dt.Kind switch
            {
                DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(dt, tz),
                DateTimeKind.Local => dt,
                _ => dt // Unspecified: already a local wall clock time
            };
        }

        var startLocalWall = NormalizeToLocalWall(startLocal);
        var endLocalWall = NormalizeToLocalWall(endLocal);

        var duration = endLocalWall - startLocalWall;
        if (duration < TimeSpan.FromMinutes(15)) return (false, "Minimum booking duration is 15 minutes.", null);
        if (duration > TimeSpan.FromHours(3)) return (false, "Maximum booking duration is 3 hours.", null);

        // Convert normalized local wall time to UTC for storage
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(startLocalWall, DateTimeKind.Unspecified), tz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(endLocalWall, DateTimeKind.Unspecified), tz);

        // Per-room lock for race-free check+add
        object roomLock;
        lock (_roomLock)
        {
            if (!_roomLocks.TryGetValue(roomId, out roomLock!))
            {
                roomLock = new object();
                _roomLocks[roomId] = roomLock;
            }
        }

        lock (roomLock)
        {
            // Validate room exists
            var roomExists = _rooms.Any(r => r.Id == roomId);
            if (!roomExists) return (false, "Room not found.", null);

            // Collision check
            lock (_bookings)
            {
                var hasOverlap = _bookings.Any(b => b.RoomId == roomId && b.EndUtc > startUtc && b.StartUtc < endUtc);
                if (hasOverlap)
                {
                    return (false, "Time slot overlaps with an existing booking.", null);
                }

                var booking = new Booking
                {
                    RoomId = roomId,
                    Title = title.Trim(),
                    StartUtc = startUtc,
                    EndUtc = endUtc,
                    CreatedByLogin = createdByLogin
                };
                _bookings.Add(booking);
                return (true, null, booking);
            }
        }
    }
}
