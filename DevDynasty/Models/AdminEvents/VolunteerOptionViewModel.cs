namespace DevDynasty.Models.AdminEvents
{
    public class VolunteerOptionViewModel
    {
        public Guid VolunteerId { get; set; }
        public string VolunteerFirstName { get; set; } = string.Empty;
        public string VolunteerSurname { get; set; } = string.Empty;
        public string? VolunteerEmail { get; set; }

        public string FullName => $"{VolunteerFirstName} {VolunteerSurname}".Trim();
    }
}