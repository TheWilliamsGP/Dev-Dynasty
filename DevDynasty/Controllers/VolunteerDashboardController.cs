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
            ViewData["Title"] = "Volunteer Dashboard";

            var model = await _dashboardService.GetDashboardAsync(volunteerId);

            if (model == null)
                return NotFound();

            return View(model);
        }

        [HttpGet("events/{volunteerId}")]
        public async Task<IActionResult> Events(Guid volunteerId)
        {
            ViewData["Title"] = "Volunteer Events";

            var model = await _dashboardService.GetDashboardAsync(volunteerId);

            if (model == null)
                return NotFound();

            return View(model);
        }

        [HttpGet("events/details/{volunteerId}/{eventId}")]
        public async Task<IActionResult> EventDetails(Guid volunteerId, Guid eventId)
        {
            ViewData["Title"] = "Event Details";

            var model = await _dashboardService.GetEventDetailsAsync(volunteerId, eventId);

            if (model == null)
                return NotFound();

            return View(model);
        }

        [HttpPost("events/join")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinEvent(Guid volunteerId, Guid eventId)
        {
            await _dashboardService.VolunteerForEventAsync(volunteerId, eventId);

            return RedirectToAction(nameof(EventDetails), new
            {
                volunteerId,
                eventId
            });
        }
    }
}