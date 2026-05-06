using DevDynasty.Models.AdminEvents;
using DevDynasty.Services;
using Microsoft.AspNetCore.Mvc;

namespace DevDynasty.Controllers
{
    [Route("admin/events")]
    public class AdminEventsController : Controller
    {
        private readonly SupabaseAdminEventsService _eventsService;

        public AdminEventsController(SupabaseAdminEventsService eventsService)
        {
            _eventsService = eventsService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Admin Events";
            var events = await _eventsService.GetEventsAsync();
            return View(events);
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> Details(Guid id)
        {
            ViewData["Title"] = "View Event";

            var selectedEvent = await _eventsService.GetEventByIdAsync(id);

            if (selectedEvent == null)
                return NotFound();

            var assignedVolunteers = await _eventsService.GetAssignedVolunteersAsync(id);

            var model = new EventDetailsViewModel
            {
                Event = selectedEvent,
                AssignedVolunteers = assignedVolunteers
            };

            return View(model);
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Create Event";

            var model = new AdminEventFormViewModel
            {
                Locations = await _eventsService.GetLocationsAsync(),
                RequiredVolunteers = 1,
                EventStatus = "active"
            };

            return View(model);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminEventFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Locations = await _eventsService.GetLocationsAsync();
                return View(model);
            }

            await _eventsService.CreateEventAsync(model);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            ViewData["Title"] = "Edit Event";

            var model = await _eventsService.GetEventFormByIdAsync(id);

            if (model == null)
                return NotFound();

            return View(model);
        }

        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, AdminEventFormViewModel model)
        {
            model.EventId = id;

            if (!ModelState.IsValid)
            {
                model.Locations = await _eventsService.GetLocationsAsync();
                return View(model);
            }

            await _eventsService.UpdateEventAsync(model);

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            ViewData["Title"] = "Delete Event";

            var selectedEvent = await _eventsService.GetEventByIdAsync(id);

            if (selectedEvent == null)
                return NotFound();

            return View(selectedEvent);
        }

        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _eventsService.DeleteEventAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("manage-volunteers/{id}")]
        public async Task<IActionResult> ManageVolunteers(Guid id)
        {
            ViewData["Title"] = "Manage Event Volunteers";

            var model = await _eventsService.GetManageVolunteersModelAsync(id);

            if (model == null)
                return NotFound();

            return View(model);
        }

        [HttpPost("assign-volunteer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignVolunteer(Guid eventId, Guid volunteerId)
        {
            await _eventsService.AssignVolunteerAsync(eventId, volunteerId);
            return RedirectToAction(nameof(ManageVolunteers), new { id = eventId });
        }

        [HttpPost("unassign-volunteer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnassignVolunteer(Guid eventId, Guid volunteerId)
        {
            await _eventsService.UnassignVolunteerAsync(eventId, volunteerId);
            return RedirectToAction(nameof(ManageVolunteers), new { id = eventId });
        }
    }
}