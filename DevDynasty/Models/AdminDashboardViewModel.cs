namespace DevDynasty.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalAmount { get; set; }
        public int TotalDonations { get; set; }
        public int TotalCardPayments { get; set; }
        public int TotalEftPayments { get; set; }
        public List<DonationDto> RecentDonations { get; set; }
    }
}
