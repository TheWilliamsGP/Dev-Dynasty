using Microsoft.AspNetCore.Mvc;
using DevDynasty.Models;
using DevDynasty.Services;
using System.Text.Json;

namespace DevDynasty.Controllers
{
    public class DonationUserController : Controller
    {
        private readonly SupabaseService _supabase;

        public DonationUserController(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        //Donation page
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(UserDonationViewModel model)
        {
            TempData["Donation"] = JsonSerializer.Serialize(model);
            return RedirectToAction("DonorDetails");
        }

        //Donor details
        public IActionResult DonorDetails()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DonorDetails(UserDonorViewModel donorModel)
        {
            var donationData = JsonSerializer.Deserialize<UserDonationViewModel>(
                TempData["Donation"]?.ToString()
            );

            if (donationData == null)
            {
                return RedirectToAction("Index");
            }

            var donor = await _supabase.CreateDonor(new
            {
                donarname = donorModel.DonorName,
                donaremail = donorModel.DonorEmail,
                field = donorModel.Field
            });

            if (donor?.donarid == null)
            {
                throw new Exception("Donor ID was not returned from Supabase");
            }

            //Store donor for payment step
            TempData["Donor"] = JsonSerializer.Serialize(donor);
            TempData.Keep("Donation");

            return RedirectToAction("Payment");
        }

        //Payment step
        public IActionResult Payment()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Payment(UserPaymentViewModel model)
        {
            var donationData = JsonSerializer.Deserialize<UserDonationViewModel>(
                TempData["Donation"]?.ToString()
            );

            var donor = JsonSerializer.Deserialize<DonorDto>(
                TempData["Donor"]?.ToString()
            );

            // Keep TempData alive for this request
            TempData.Keep("Donation");
            TempData.Keep("Donor");

            if (donationData == null || donor?.donarid == null)
            {
                return RedirectToAction("Index");
            }

            //If user chooses credit card
            if (model.PaymentType == "Card")
            {
                if (model.SaveCard)
                {
                    //Validate card number
                    if (!long.TryParse(model.CardNumber, out long parsedCard))
                    {
                        ModelState.AddModelError("", "Invalid card number");
                        return View(model);
                    }

                    //Validate expiry
                    if (string.IsNullOrEmpty(model.Expiry) || !model.Expiry.Contains("/"))
                    {
                        ModelState.AddModelError("", "Invalid expiry format");
                        return View(model);
                    }

                    var expiryParts = model.Expiry.Split('/');

                    var card = new CardDto
                    {
                        donarid = donor.donarid.Value,
                        cardnumber = parsedCard,
                        cardexpiry = new DateTime(
                            int.Parse("20" + expiryParts[1]), //Year
                            int.Parse(expiryParts[0]),        //Month
                            1
                        ),
                        cardjoindate = DateTime.UtcNow,
                        cardtype = model.CardType
                    };

                    await _supabase.CreateCard(card);
                }
            }

            //Create donation
            await _supabase.CreateDonation(new DonationDto
            {
                donarid = donor.donarid.Value,
                donationamount = donationData.DonationAmount,
                ismonetary = true,
                donationdate = DateTime.UtcNow,
                donationcontent = model.PaymentType == "EFT"
                    ? "EFT - Pending"
                    : "Card Payment",
                isanonymous = donationData.IsAnonymous
            });

            return RedirectToAction("Success");
        }

        //Donation created successfully
        public IActionResult Success()
        {
            return View();
        }
    }
}