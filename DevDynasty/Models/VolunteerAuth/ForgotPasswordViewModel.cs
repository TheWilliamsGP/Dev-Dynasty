using System.ComponentModel.DataAnnotations;

namespace DevDynasty.Models.VolunteerAuth
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;
    }
}