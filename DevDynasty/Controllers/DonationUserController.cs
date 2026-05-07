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

        //donations page
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(UserDonationViewModel model)
        {
            // Money donation validation
            if (model.IsMonetary)
            {
                if (!model.DonationAmount.HasValue || model.DonationAmount <= 0)
                {
                    ModelState.AddModelError(
                        "DonationAmount",
                        "Please enter a valid donation amount."
                    );
                }
            }

            // Goods donation validation
            else
            {
                if (string.IsNullOrWhiteSpace(model.DonationContent))
                {
                    ModelState.AddModelError(
                        "DonationContent",
                        "Please describe the donated goods."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            TempData["Donation"] = JsonSerializer.Serialize(model);

            return RedirectToAction("DonorDetails");
        }

        //donor details
        public IActionResult DonorDetails()
        {
            var donationData = JsonSerializer.Deserialize<UserDonationViewModel>(
                TempData["Donation"]?.ToString()
            );

            if (donationData == null)
            {
                return RedirectToAction("Index");
            }

            TempData.Keep("Donation");

            ViewBag.IsMonetary = donationData.IsMonetary;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DonorDetails(UserDonorViewModel donorModel)
        {
            if (!ModelState.IsValid)
            {
                return View(donorModel);
            }
            var donationData = JsonSerializer.Deserialize<UserDonationViewModel>(
                TempData["Donation"]?.ToString()
            );

            if (donationData == null)
            {
                return RedirectToAction("Index");
            }

            //creates donor
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

            //stores donor for the next step
            TempData["Donor"] = JsonSerializer.Serialize(donor);
            TempData.Keep("Donation");

            //monetary donation
            if (donationData.IsMonetary)
            {
                return RedirectToAction("Payment");
            }

            //non-monetary donation
            await _supabase.CreateDonation(new DonationDto
            {
                donarid = donor.donarid.Value,

                donationamount = 0,

                ismonetary = false,

                donationdate = DateTime.UtcNow,

                donationcontent = donationData.DonationContent,

                isanonymous = donationData.IsAnonymous
            });

            return RedirectToAction("Success");
        }

        //payment page
        public IActionResult Payment()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Payment(UserPaymentViewModel model)
        {
            // Card validation only if Card payment selected
            if (model.PaymentType == "Card")
            {
                if (string.IsNullOrWhiteSpace(model.CardNumber))
                {
                    ModelState.AddModelError(
                        "CardNumber",
                        "Card number is required."
                    );
                }

                if (string.IsNullOrWhiteSpace(model.Expiry))
                {
                    ModelState.AddModelError(
                        "Expiry",
                        "Expiry date is required."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var donationData = JsonSerializer.Deserialize<UserDonationViewModel>(
                TempData["Donation"]?.ToString()
            );

            var donor = JsonSerializer.Deserialize<DonorDto>(
                TempData["Donor"]?.ToString()
            );

            TempData.Keep("Donation");
            TempData.Keep("Donor");

            if (donationData == null || donor?.donarid == null)
            {
                return RedirectToAction("Index");
            }

            //card payment
            if (model.PaymentType == "Card")
            {
                //save card if checked
                if (model.SaveCard)
                {
                    //card number validation
                    if (!long.TryParse(model.CardNumber, out long parsedCard))
                    {
                        ModelState.AddModelError("", "Invalid card number");
                        return View(model);
                    }

                    //Expiry validation
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
                            int.Parse("20" + expiryParts[1]),
                            int.Parse(expiryParts[0]),
                            1
                        ),

                        cardjoindate = DateTime.UtcNow,

                        cardtype = model.CardType
                    };

                    await _supabase.CreateCard(card);
                }
            }

            //Monetary donation creation
            await _supabase.CreateDonation(new DonationDto
            {
                donarid = donor.donarid.Value,

                donationamount = donationData.DonationAmount ?? 0,

                ismonetary = true,

                donationdate = DateTime.UtcNow,

                donationcontent = "Monetary Donation",

                isanonymous = donationData.IsAnonymous
            });

            return RedirectToAction("Success");
        }

        //Success!
        public IActionResult Success()
        {
            return View();
        }
    }
}