using System.ComponentModel.DataAnnotations;

namespace DevDynasty.Models
{
    public class UserDonorViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        public string DonorName { get; set; }

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email.")]
        public string DonorEmail { get; set; }

        [Required(ErrorMessage = "Country is required.")]
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
