using Microsoft.AspNetCore.Mvc;

namespace ConferenceBooking.Web.Controllers;

[Route("Account")]
public class AccountController : Controller
{
    /// <summary>
    /// Logs user in based on the {login} route parameter and redirects to the main calendar view.
    /// GET /Account/Login/{login}
    /// </summary>
    [HttpGet("Login/{login}")]
    public IActionResult Login(string login)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            // Validate required login parameter.
            return BadRequest("Login cannot be empty.");
        }

        // Simple role mechanism: 'admin' => Admin, everything else => User.
        var role = string.Equals(login, "admin", StringComparison.OrdinalIgnoreCase)
            ? "Admin"
            : "User";

        HttpContext.Session.SetString("UserLogin", login);
        HttpContext.Session.SetString("UserRole", role);

        return RedirectToAction("Calendar", "Booking");
    }

    /// <summary>
    /// Logs current user out by clearing session and redirects to the calendar view.
    /// GET /Account/Logout
    /// </summary>
    [HttpGet("Logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Calendar", "Booking");
    }
}
