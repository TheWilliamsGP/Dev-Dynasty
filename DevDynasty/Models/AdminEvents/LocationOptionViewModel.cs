namespace DevDynasty.Models.AdminEvents
{
    public class LocationOptionViewModel
    {
        public Guid LocationId { get; set; }
        public string? LocationAddress { get; set; }
        public int? LocationCapacity { get; set; }

        public string DisplayName
        {
            get
            {
                if (LocationCapacity.HasValue)
                    return $"{LocationAddress} - Capacity: {LocationCapacity.Value}";

                return LocationAddress ?? "Unnamed location";
            }
        }
    }
}