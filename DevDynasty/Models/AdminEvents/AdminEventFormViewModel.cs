using System.ComponentModel.DataAnnotations;

namespace DevDynasty.Models.AdminEvents
{
    public class AdminEventFormViewModel
    {
        public Guid? EventId { get; set; }

        [Required]
        [Display(Name = "Event name")]
        public string EventName { get; set; } = string.Empty;

        [Display(Name = "Event type")]
        public string? EventType { get; set; }

        [Display(Name = "Description")]
        public string? EventDescription { get; set; }

        [Display(Name = "Start date")]
        public string? EventStartDate { get; set; }

        [Display(Name = "End date")]
        public string? EventEndDate { get; set; }

        [Display(Name = "Location")]
        public Guid? LocationId { get; set; }

        [Range(1, 500)]
        [Display(Name = "Required volunteers")]
        public int RequiredVolunteers { get; set; } = 1;

        [Display(Name = "Status")]
        public string EventStatus { get; set; } = "active";

        public List<LocationOptionViewModel> Locations { get; set; } = new();
    }
}