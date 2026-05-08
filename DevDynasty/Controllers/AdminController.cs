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
            if (!IsCurrentAdmin())
                return RedirectToAction("Login", "Account");

            try
            {
                ViewData["Title"] = "Admin Dashboard";

                var donations = await _supabase.GetDonations();
                var cards = await _supabase.GetCards();

                var totalAmount = donations
                    .Where(d => d.ismonetary)
                    .Sum(d => d.donationamount);

                var totalDonations = donations.Count;

                var totalCredit = cards.Count(c =>
                    string.Equals(c.cardtype, "Credit", StringComparison.OrdinalIgnoreCase));

                var totalDebit = cards.Count(c =>
                    string.Equals(c.cardtype, "Debit", StringComparison.OrdinalIgnoreCase));

                var totalGoods = donations.Count(d => !d.ismonetary);

                var model = new AdminDashboardViewModel
                {
                    TotalAmount = totalAmount,
                    TotalDonations = totalDonations,
                    TotalCardPayments = totalCredit,
                    TotalEftPayments = totalDebit,
                    TotalGoodsDonations = totalGoods,
                    RecentDonations = donations
                        .OrderByDescending(d => d.donationdate)
                        .Take(5)
                        .ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = FriendlyAdminError(ex.Message);

                var model = new AdminDashboardViewModel
                {
                    TotalAmount = 0,
                    TotalDonations = 0,
                    TotalCardPayments = 0,
                    TotalEftPayments = 0,
                    TotalGoodsDonations = 0,
                    RecentDonations = new List<DonationDto>()
                };

                return View(model);
            }
        }

        public async Task<IActionResult> Donors()
        {
            if (!IsCurrentAdmin())
                return RedirectToAction("Login", "Account");

            try
            {
                ViewData["Title"] = "Admin Donors";

                var donors = await _supabase.GetDonors();
                return View(donors);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = FriendlyAdminError(ex.Message);
                return View(new List<DonorDto>());
            }
        }

        public async Task<IActionResult> Donations()
        {
            if (!IsCurrentAdmin())
                return RedirectToAction("Login", "Account");

            try
            {
                ViewData["Title"] = "Admin Donations";

                var donations = await _supabase.GetDonations();
                var donors = await _supabase.GetDonors();

                var model = donations.Select(d => new AdminDonationViewModel
                {
                    Amount = d.donationamount,
                    Date = d.donationdate,
                    Content = d.donationcontent,
                    IsAnonymous = d.isanonymous,
                    IsMonetary = d.ismonetary,
                    DonorName = donors
                        .FirstOrDefault(x => x.donarid == d.donarid)
                        ?.donarname
                }).ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = FriendlyAdminError(ex.Message);
                return View(new List<AdminDonationViewModel>());
            }
        }

        public async Task<IActionResult> Payments()
        {
            if (!IsCurrentAdmin())
                return RedirectToAction("Login", "Account");

            try
            {
                ViewData["Title"] = "Admin Payments";

                var cards = await _supabase.GetCards();
                var donors = await _supabase.GetDonors();

                var model = cards.Select(c => new AdminPaymentViewModel
                {
                    CardType = c.cardtype,
                    CardNumber = c.cardnumber,
                    Expiry = c.cardexpiry,
                    DonorName = donors
                        .FirstOrDefault(d => d.donarid == c.donarid)
                        ?.donarname
                }).ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = FriendlyAdminError(ex.Message);
                return View(new List<AdminPaymentViewModel>());
            }
        }

        private bool IsCurrentAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            var adminId = HttpContext.Session.GetString("AdminId");

            return role == "Admin" && !string.IsNullOrWhiteSpace(adminId);
        }

        private static string FriendlyAdminError(string error)
        {
            var lower = error.ToLowerInvariant();

            if (lower.Contains("requested path is invalid"))
                return "The admin dashboard could not load because a Supabase REST path is incorrect.";

            if (lower.Contains("unauthorized") || lower.Contains("401"))
                return "The app could not connect to Supabase. Please check the Supabase API key.";

            if (lower.Contains("permission denied") || lower.Contains("rls"))
                return "Supabase blocked access to this data. Please check table permissions or RLS policies.";

            if (lower.Contains("relation") && lower.Contains("does not exist"))
                return "One of the Supabase tables used by the admin dashboard does not exist.";

            if (lower.Contains("network") || lower.Contains("timeout"))
                return "Network error while loading admin data. Please try again.";

            return $"Something went wrong while loading admin data. Details: {error}";
        }
    }
}