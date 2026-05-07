namespace DevDynasty.Models
{
    public class AdminPaymentViewModel
    {
        public string DonorName { get; set; }
        public long CardNumber { get; set; }
        public string CardType { get; set; }
        public DateTime Expiry {  get; set; }

    }
}
