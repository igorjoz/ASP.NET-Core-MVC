using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ConferenceBooking.Web.Filters;

/// <summary>
/// Simple session-based admin gate. If the user is not an Admin, redirect to the calendar.
/// </summary>
public class AdminOnlyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var role = context.HttpContext.Session.GetString("UserRole");
        if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToActionResult("Calendar", "Booking", null);
            return;
        }
        base.OnActionExecuting(context);
    }
}
