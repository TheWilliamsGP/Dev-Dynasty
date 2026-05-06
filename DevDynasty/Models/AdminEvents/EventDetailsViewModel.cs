namespace DevDynasty.Models.AdminEvents
{
    public class EventDetailsViewModel
    {
        public EventListItemViewModel Event { get; set; } = new();
        public List<VolunteerOptionViewModel> AssignedVolunteers { get; set; } = new();
    }
}