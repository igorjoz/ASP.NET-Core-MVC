namespace ConferenceBooking.Web.Models;

/// <summary>
/// A reservation for a specific room and time range (UTC stored).
/// </summary>
public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoomId { get; set; }
    public required string Title { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public required string CreatedByLogin { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
