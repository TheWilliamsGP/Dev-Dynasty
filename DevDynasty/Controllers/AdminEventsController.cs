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
            try
            {
                ViewData["Title"] = "Admin Events";

                var events = await _eventsService.GetEventsAsync();

                return View(events);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = FriendlyAdminError(ex.Message);
                return View(new List<EventListItemViewModel>());
            }
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                ViewData["Title"] = "View Event";

                var selectedEvent = await _eventsService.GetEventByIdAsync(id);

                if (selectedEvent == null)
                {
                    TempData["ErrorMessage"] = "The selected event could not be found.";
                    return RedirectToAction(nameof(Index));
                }

                var assignedVolunteers = await _eventsService.GetAssignedVolunteersAsync(id);

                var model = new EventDetailsViewModel
                {
                    Event = selectedEvent,
                    AssignedVolunteers = assignedVolunteers
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = FriendlyAdminError(ex.Message);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            try
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
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = FriendlyAdminError(ex.Message);

                var model = new AdminEventFormViewModel
                {
                    RequiredVolunteers = 1,
                    EventStatus = "active"
                };

                return View(model);
            }
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminEventFormViewModel model)
        {
            ViewData["Title"] = "Create Event";

            if (!ModelState.IsValid)
            {
                model.Locations = await SafeGetLocationsAsync();
                TempData["ErrorMessage"] = "Please fix the highlighted fields and try again.";
                return View(model);
            }

            try
            {
                await _eventsService.CreateEventAsync(model);

                TempData["SuccessMessage"] = "Event created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                model.Locations = await SafeGetLocationsAsync();

                var friendlyError = FriendlyAdminError(ex.Message);
                ModelState.AddModelError("", friendlyError);
                TempData["ErrorMessage"] = friendlyError;

                return View(model);
            }
        }

        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                ViewData["Title"] = "Edit Event";

                var model = await _eventsService.GetEventFormByIdAsync(id);

                if (model == null)
                {
                    TempData["ErrorMessage"] = "The selected event could not be found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = FriendlyAdminError(ex.Message);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, AdminEventFormViewModel model)
        {
            ViewData["Title"] = "Edit Event";

            model.EventId = id;

            if (!ModelState.IsValid)
            {
                model.Locations = await SafeGetLocationsAsync();
                TempData["ErrorMessage"] = "Please fix the highlighted fields and try again.";
                return View(model);
            }

            try
            {
                await _eventsService.UpdateEventAsync(model);

                TempData["SuccessMessage"] = "Event updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                model.Locations = await SafeGetLocationsAsync();

                var friendlyError = FriendlyAdminError(ex.Message);
                ModelState.AddModelError("", friendlyError);
                TempData["ErrorMessage"] = friendlyError;

                return View(model);
            }
        }

        [HttpGet("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                ViewData["Title"] = "Delete Event";

                var selectedEvent = await _eventsService.GetEventByIdAsync(id);

                if (selectedEvent == null)
                {
                    TempData["ErrorMessage"] = "The selected event could not be found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(selectedEvent);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = FriendlyAdminError(ex.Message);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                await _eventsService.DeleteEventAsync(id);

                TempData["SuccessMessage"] = "Event deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = FriendlyAdminError(ex.Message);
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        [HttpGet("manage-volunteers/{id}")]
        public async Task<IActionResult> ManageVolunteers(Guid id)
        {
            try
            {
                ViewData["Title"] = "Manage Event Volunteers";

                var model = await _eventsService.GetManageVolunteersModelAsync(id);

                if (model == null)
                {
                    TempData["ErrorMessage"] = "The selected event could not be found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = FriendlyAdminError(ex.Message);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost("assign-volunteer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignVolunteer(Guid eventId, Guid volunteerId)
        {
            try
            {
                await _eventsService.AssignVolunteerAsync(eventId, volunteerId);
                TempData["SuccessMessage"] = "Volunteer assigned successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = FriendlyAdminError(ex.Message);
            }

            return RedirectToAction(nameof(ManageVolunteers), new { id = eventId });
        }

        [HttpPost("unassign-volunteer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnassignVolunteer(Guid eventId, Guid volunteerId)
        {
            try
            {
                await _eventsService.UnassignVolunteerAsync(eventId, volunteerId);
                TempData["SuccessMessage"] = "Volunteer unassigned successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = FriendlyAdminError(ex.Message);
            }

            return RedirectToAction(nameof(ManageVolunteers), new { id = eventId });
        }

        private async Task<List<LocationOptionViewModel>> SafeGetLocationsAsync()
        {
            try
            {
                return await _eventsService.GetLocationsAsync();
            }
            catch
            {
                return new List<LocationOptionViewModel>();
            }
        }

        private static string FriendlyAdminError(string error)
        {
            var lower = error.ToLowerInvariant();

            if (lower.Contains("bucket not found"))
                return "The event image bucket was not found. Please check that the Supabase Storage bucket is named correctly.";

            if (lower.Contains("eventimageurl"))
                return "The event image column is missing. Please make sure eventimageurl exists in eventactivitytable.";

            if (lower.Contains("unauthorized") || lower.Contains("401"))
                return "The app could not connect to Supabase correctly. Please check the service role key.";

            if (lower.Contains("only jpg") || lower.Contains("only jpeg") || lower.Contains("only png") || lower.Contains("image"))
                return "Please upload a valid image file.";

            if (lower.Contains("foreign key"))
                return "One of the selected related records no longer exists. Please refresh the page and try again.";

            if (lower.Contains("requiredvolunteers"))
                return "Please enter a valid number of required volunteers.";

            if (lower.Contains("location"))
                return "There was a problem saving the location. Please choose an existing location or type a new one.";

            if (lower.Contains("network") || lower.Contains("timeout"))
                return "Network error. Please check your connection and try again.";

            return "Something went wrong while saving the event. Please try again.";
        }
    }
}