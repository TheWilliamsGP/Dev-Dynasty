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
    }
}
