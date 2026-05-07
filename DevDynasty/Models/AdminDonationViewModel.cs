namespace DevDynasty.Models
{
    public class AdminDonationViewModel
    {
        public string DonorName { get; set; }

        public int Amount { get; set; }

        public DateTime Date { get; set; }

        public string Content { get; set; }

        public bool IsAnonymous { get; set; }

        public bool IsMonetary { get; set; }
    }
}
