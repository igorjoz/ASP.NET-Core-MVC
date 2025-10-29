namespace ConferenceBooking.Web.Models;

/// <summary>
/// Simplified application user for session-based demo.
/// </summary>
public class AppUser
{
    public required string Login { get; set; }
    public required string Role { get; set; } // "Admin" or "User"
}
