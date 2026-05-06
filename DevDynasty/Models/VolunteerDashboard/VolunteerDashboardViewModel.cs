namespace DevDynasty.Models.VolunteerDashboard
{
    public class VolunteerDashboardViewModel
    {
        public Guid VolunteerId { get; set; }
        public string VolunteerName { get; set; } = string.Empty;
        public List<VolunteerEventCardViewModel> Events { get; set; } = new();
    }
}