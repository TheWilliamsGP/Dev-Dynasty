namespace DevDynasty.Models
{
    public class UserDonationViewModel
    {
        public int DonationAmount { get; set; }
        public bool IsAnonymous { get; set; }
    }

    public class UserDonorViewModel
    {
        public string DonorName { get; set; }
        public string DonorEmail { get; set; }
        public string Field { get; set; }
    }

    public class DonationDto
    {
        public Guid? donarid { get; set; }            
        public DateTime donationdate { get; set; }
        public bool ismonetary { get; set; }         
        public string donationcontent { get; set; }   
        public int donationamount { get; set; }
    }

    public class DonorDto
    {
        public Guid? donorid { get; set; }
        public string donorname { get; set; }
        public string donoremail { get; set; }
        public string field { get; set; }
    }
}