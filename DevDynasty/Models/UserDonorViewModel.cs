namespace DevDynasty.Models
{
    public class UserDonorViewModel
    {
        public string DonorName { get; set; }
        public string DonorEmail { get; set; }
        public string Field { get; set; }
    }

    public class DonorDto
    {
        public Guid? donarid { get; set; }
        public string donarname { get; set; }
        public string donaremail { get; set; }
        public string field { get; set; }
    }

    public class DonorInsertDto
    {
        public string donarname { get; set; }
        public string donaremail { get; set; }
        public string field { get; set; }
    }
}
