namespace DevDynasty.Models
{
    public class UserDonationViewModel
    {
        public bool IsMonetary { get; set; }

        // Monetary
        public int? DonationAmount { get; set; }

        // Non-monetary
        public string DonationContent { get; set; }

        // Shared
        public bool IsAnonymous { get; set; }
    }

    public class DonationDto
    {
        public Guid? donarid { get; set; }
        public DateTime donationdate { get; set; }

        public bool ismonetary { get; set; }

        public string donationcontent { get; set; }

        public int donationamount { get; set; }

        public bool isanonymous { get; set; }
    }
}