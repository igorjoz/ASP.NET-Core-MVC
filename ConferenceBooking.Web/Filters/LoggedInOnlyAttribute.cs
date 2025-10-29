using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ConferenceBooking.Web.Filters;

/// <summary>
/// Simple session-based gate for logged-in users.
/// </summary>
public class LoggedInOnlyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var login = context.HttpContext.Session.GetString("UserLogin");
        if (string.IsNullOrWhiteSpace(login))
        {
            // If not logged in, go to the public landing page with login choices.
            context.Result = new RedirectToActionResult("Index", "Home", null);
            return;
        }
        base.OnActionExecuting(context);
    }
}
