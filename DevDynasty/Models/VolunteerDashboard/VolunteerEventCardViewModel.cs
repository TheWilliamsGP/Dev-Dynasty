namespace DevDynasty.Models.VolunteerDashboard
{
    public class VolunteerEventCardViewModel
    {
        public Guid EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? EventType { get; set; }
        public string? EventDescription { get; set; }
        public string? EventStartDate { get; set; }
        public string? EventEndDate { get; set; }
        public string? LocationAddress { get; set; }
        public int RequiredVolunteers { get; set; }
        public int JoinedVolunteers { get; set; }
        public bool HasJoined { get; set; }
        public string? EventImageUrl { get; set; }

        public string StaffingStatus
        {
            get
            {
                if (DateTime.TryParse(EventEndDate, out var endDate) && endDate.Date < DateTime.Today)
                    return "Completed";

                if (JoinedVolunteers == 0)
                    return "No volunteers yet";

                if (JoinedVolunteers < RequiredVolunteers)
                    return "Needs volunteers";

                return "Fully staffed";
            }
        }

        public bool CanVolunteer
        {
            get
            {
                if (HasJoined)
                    return false;

                if (DateTime.TryParse(EventEndDate, out var endDate) && endDate.Date < DateTime.Today)
                    return false;

                return true;
            }
        }
    }
}