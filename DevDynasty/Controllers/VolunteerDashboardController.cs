using DevDynasty.Services;
using Microsoft.AspNetCore.Mvc;

namespace DevDynasty.Controllers
{
    [Route("volunteer")]
    public class VolunteerDashboardController : Controller
    {
        private readonly SupabaseVolunteerDashboardService _dashboardService;

        public VolunteerDashboardController(SupabaseVolunteerDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("dashboard/{volunteerId}")]
        public async Task<IActionResult> Dashboard(Guid volunteerId)
        {
            if (!IsCurrentVolunteer(volunteerId))
                return RedirectToAction("Login", "Account");

            try
            {
                ViewData["Title"] = "Volunteer Dashboard";

                var model = await _dashboardService.GetDashboardAsync(volunteerId);

                if (model == null)
                {
                    TempData["ErrorMessage"] = "Your volunteer profile could not be found. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                return View(model);
            }
            catch
            {
                TempData["ErrorMessage"] = "Something went wrong while loading your dashboard. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet("events/{volunteerId}")]
        public async Task<IActionResult> Events(Guid volunteerId)
        {
            if (!IsCurrentVolunteer(volunteerId))
                return RedirectToAction("Login", "Account");

            try
            {
                ViewData["Title"] = "Volunteer Events";

                var model = await _dashboardService.GetDashboardAsync(volunteerId);

                if (model == null)
                {
                    TempData["ErrorMessage"] = "Your volunteer profile could not be found. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                return View(model);
            }
            catch
            {
                TempData["ErrorMessage"] = "Something went wrong while loading events. Please try again.";
                return RedirectToAction(nameof(Dashboard), new { volunteerId });
            }
        }

        [HttpGet("events/details/{volunteerId}/{eventId}")]
        public async Task<IActionResult> EventDetails(Guid volunteerId, Guid eventId)
        {
            if (!IsCurrentVolunteer(volunteerId))
                return RedirectToAction("Login", "Account");

            try
            {
                ViewData["Title"] = "Event Details";

                var model = await _dashboardService.GetEventDetailsAsync(volunteerId, eventId);

                if (model == null)
                {
                    TempData["ErrorMessage"] = "The selected event could not be found.";
                    return RedirectToAction(nameof(Events), new { volunteerId });
                }

                return View(model);
            }
            catch
            {
                TempData["ErrorMessage"] = "Something went wrong while loading the event details. Please try again.";
                return RedirectToAction(nameof(Events), new { volunteerId });
            }
        }

        [HttpPost("events/join")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinEvent(Guid volunteerId, Guid eventId)
        {
            if (!IsCurrentVolunteer(volunteerId))
                return RedirectToAction("Login", "Account");

            try
            {
                await _dashboardService.VolunteerForEventAsync(volunteerId, eventId);
                TempData["SuccessMessage"] = "You have successfully volunteered for this event.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Something went wrong while volunteering for this event. Please try again.";
            }

            return RedirectToAction(nameof(EventDetails), new
            {
                volunteerId,
                eventId
            });
        }

        [HttpPost("events/unvolunteer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnvolunteerEvent(Guid volunteerId, Guid eventId)
        {
            if (!IsCurrentVolunteer(volunteerId))
                return RedirectToAction("Login", "Account");

            try
            {
                await _dashboardService.UnvolunteerFromEventAsync(volunteerId, eventId);
                TempData["SuccessMessage"] = "You have removed yourself from this event.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Something went wrong while removing you from this event. Please try again.";
            }

            return RedirectToAction(nameof(EventDetails), new
            {
                volunteerId,
                eventId
            });
        }

        private bool IsCurrentVolunteer(Guid volunteerId)
        {
            var sessionVolunteerId = HttpContext.Session.GetString("VolunteerId");

            return Guid.TryParse(sessionVolunteerId, out var currentVolunteerId)
                && currentVolunteerId == volunteerId;
        }
    }
}