namespace ConferenceBooking.Web.Models;

/// <summary>
/// Conference room entity stored in memory.
/// </summary>
public class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public int Capacity { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
