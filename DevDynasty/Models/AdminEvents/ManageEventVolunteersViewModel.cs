namespace DevDynasty.Models.AdminEvents
{
    public class ManageEventVolunteersViewModel
    {
        public EventListItemViewModel Event { get; set; } = new();
        public List<VolunteerOptionViewModel> AssignedVolunteers { get; set; } = new();
        public List<VolunteerOptionViewModel> AvailableVolunteers { get; set; } = new();
    }
}