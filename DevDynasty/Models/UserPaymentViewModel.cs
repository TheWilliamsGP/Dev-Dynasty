namespace DevDynasty.Models
{
    public class UserPaymentViewModel
    {
        public string PaymentType { get; set; } //Credit or EFT
        public string CardType { get; set; } //Credit or Debit
        public string CardNumber { get; set; }
        public string Expiry { get; set; }
        public bool SaveCard { get; set; }
    }

    public class CardDto
    {
        public Guid donarid { get; set; }
        public long cardnumber { get; set; }
        public DateTime cardexpiry { get; set; }
        public DateTime cardjoindate { get; set; }
        public string cardtype { get; set; }
    }
}
