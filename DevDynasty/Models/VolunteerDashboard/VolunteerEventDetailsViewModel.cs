namespace DevDynasty.Models.VolunteerDashboard
{
    public class VolunteerEventDetailsViewModel
    {
        public Guid VolunteerId { get; set; }
        public string VolunteerName { get; set; } = string.Empty;
        public VolunteerEventCardViewModel Event { get; set; } = new();
    }
}