using Microsoft.AspNetCore.Mvc;
using DevDynasty.Models;
using DevDynasty.Services;
using System.Text.Json.Serialization;

namespace DevDynasty.Controllers
{
    public class DonationUserController : Controller
    {
        private readonly SupabaseService _supabase;

        public DonationUserController(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        // Donation page
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(UserDonationViewModel model)
        {
            TempData["Donation"] = System.Text.Json.JsonSerializer.Serialize(model);
            return RedirectToAction("DonorDetails");
        }

        //Donor Details
        public IActionResult DonorDetails()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DonorDetails(UserDonorViewModel donorModel)
        {
            var donationData = System.Text.Json.JsonSerializer.Deserialize<UserDonationViewModel>(
                TempData["Donation"]?.ToString()
            );

            var donor = await _supabase.CreateDonor(new DonorDto
            {
                donorname = donorModel.DonorName,
                donoremail = donorModel.DonorEmail,
                field = donorModel.Field
            });

            if (donor?.donorid == null)
            {
                throw new Exception("Donor ID was not returned from Supabase");
            }


            await _supabase.CreateDonation(new DonationDto
            {
                donarid = donor.donorid.Value,
                donationamount = donationData.DonationAmount,
                ismonetary = true,
                donationdate = DateTime.UtcNow,
                donationcontent = "General Donation"
            });

            return RedirectToAction("Success");
        }

        public IActionResult Success()
        {
            return View();
        }
    }
}
