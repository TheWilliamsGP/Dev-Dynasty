namespace DevDynasty.Models.AdminEvents
{
    public class EventListItemViewModel
    {
        public Guid EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? EventType { get; set; }
        public string? EventDescription { get; set; }
        public string? EventStartDate { get; set; }
        public string? EventEndDate { get; set; }
        public Guid? LocationId { get; set; }
        public string? LocationAddress { get; set; }
        public int RequiredVolunteers { get; set; }
        public int JoinedVolunteers { get; set; }
        public string EventStatus { get; set; } = "active";

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
    }
}