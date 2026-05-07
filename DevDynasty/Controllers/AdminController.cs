using DevDynasty.Models;
using DevDynasty.Services;
using Microsoft.AspNetCore.Mvc;

namespace DevDynasty.Controllers
{
    public class AdminController : Controller
    {
        private readonly SupabaseService _supabase;
        public AdminController(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        public async Task<IActionResult> Dashboard()
        {
            var donations = await _supabase.GetDonations();

            var totalAmount = donations.Sum(d => d.donationamount);
            var totalDonations = donations.Count;
            var totalCard = donations.Count(d => d.donationcontent.Contains("Card"));
            var totalEft = donations.Count(d => d.donationcontent.Contains("EFT"));

            var model = new AdminDashboardViewModel
            {
                TotalAmount = totalAmount,
                TotalDonations = totalDonations,
                TotalCardPayments = totalCard,
                TotalEftPayments = totalEft,
                RecentDonations = donations.OrderByDescending(d => d.donationdate).Take(5).ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> Donors()
        {
            var donors = await _supabase.GetDonors();
            return View(donors);
        }

        public async Task<IActionResult> Donations()
        {
            var donations = await _supabase.GetDonations();
            var donors = await _supabase.GetDonors();

            var model = donations.Select(d => new AdminDonationViewModel
            {
                Amount = d.donationamount,
                Date = d.donationdate,
                Content = d.donationcontent,
                IsAnonymous = d.isanonymous,
                DonorName = donors.FirstOrDefault(x => x.donarid == d.donarid)?.donarname
            }).ToList();

            return View(model);
        }

        public async Task<IActionResult> Payments()
        {
            var cards = await _supabase.GetCards();
            var donors = await _supabase.GetDonors();

            var model = cards.Select(c => new AdminPaymentViewModel
            {
                CardType = c.cardtype,
                CardNumber = c.cardnumber,
                Expiry = c.cardexpiry,
                DonorName = donors.FirstOrDefault(d => d.donarid == c.donarid)?.donarname
            }).ToList();

            return View(model);
        }
    }
}
