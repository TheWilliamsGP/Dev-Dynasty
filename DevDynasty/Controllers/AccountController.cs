using DevDynasty.Models.VolunteerAuth;
using DevDynasty.Services;
using Microsoft.AspNetCore.Mvc;

namespace DevDynasty.Controllers
{
    [Route("account")]
    public class AccountController : Controller
    {
        private readonly SupabaseVolunteerAuthService _authService;

        public AccountController(SupabaseVolunteerAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet("register")]
        public IActionResult Register()
        {
            ViewData["Title"] = "Volunteer Register";
            return View(new VolunteerRegisterViewModel());
        }

        [HttpPost("register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(VolunteerRegisterViewModel model)
        {
            ViewData["Title"] = "Volunteer Register";

            NormalizeRegisterModel(model);

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fix the highlighted fields and try again.";
                return View(model);
            }

            try
            {
                var result = await _authService.RegisterVolunteerAsync(model);

                if (!result.IsSuccess || !result.VolunteerId.HasValue)
                {
                    ModelState.AddModelError("", result.ErrorMessage ?? "Registration failed. Please try again.");
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Registration failed. Please try again.";
                    return View(model);
                }

                SaveVolunteerSession(result.VolunteerId.Value, result.AccessToken);

                TempData["SuccessMessage"] = "Registration successful. Welcome!";
                return RedirectToAction("Dashboard", "VolunteerDashboard", new { volunteerId = result.VolunteerId.Value });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", FriendlyAuthError(ex.Message));
                TempData["ErrorMessage"] = FriendlyAuthError(ex.Message);
                return View(model);
            }
            catch
            {
                ModelState.AddModelError("", "Something went wrong while creating your account. Please try again.");
                TempData["ErrorMessage"] = "Something went wrong while creating your account. Please try again.";
                return View(model);
            }
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            ViewData["Title"] = "Volunteer Login";
            return View(new VolunteerLoginViewModel());
        }

        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(VolunteerLoginViewModel model)
        {
            ViewData["Title"] = "Volunteer Login";

            model.Email = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please enter your email and password.";
                return View(model);
            }

            try
            {
                var result = await _authService.LoginVolunteerAsync(model);

                if (!result.IsSuccess || !result.VolunteerId.HasValue)
                {
                    ModelState.AddModelError("", result.ErrorMessage ?? "Invalid email or password.");
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Invalid email or password.";
                    return View(model);
                }

                SaveVolunteerSession(result.VolunteerId.Value, result.AccessToken);

                TempData["SuccessMessage"] = "Login successful.";
                return RedirectToAction("Dashboard", "VolunteerDashboard", new { volunteerId = result.VolunteerId.Value });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", FriendlyAuthError(ex.Message));
                TempData["ErrorMessage"] = FriendlyAuthError(ex.Message);
                return View(model);
            }
            catch
            {
                ModelState.AddModelError("", "Something went wrong while logging in. Please try again.");
                TempData["ErrorMessage"] = "Something went wrong while logging in. Please try again.";
                return View(model);
            }
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            Response.Cookies.Delete("VolunteerId");
            Response.Cookies.Delete("VolunteerAccessToken");

            TempData["SuccessMessage"] = "You have been logged out.";
            return RedirectToAction("Index", "Home");
        }

        private void SaveVolunteerSession(Guid volunteerId, string? accessToken)
        {
            HttpContext.Session.SetString("VolunteerId", volunteerId.ToString());

            Response.Cookies.Append("VolunteerId", volunteerId.ToString(), new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                Response.Cookies.Append("VolunteerAccessToken", accessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddHours(8)
                });
            }
        }

        private static void NormalizeRegisterModel(VolunteerRegisterViewModel model)
        {
            model.FirstName = model.FirstName?.Trim() ?? string.Empty;
            model.Surname = model.Surname?.Trim() ?? string.Empty;
            model.Email = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            model.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber)
                ? null
                : model.PhoneNumber.Trim().Replace(" ", "").Replace("-", "");
        }

        private static string FriendlyAuthError(string error)
        {
            var lower = error.ToLowerInvariant();

            if (lower.Contains("already registered") ||
                lower.Contains("already exists") ||
                lower.Contains("user already") ||
                lower.Contains("email_exists") ||
                lower.Contains("duplicate key"))
            {
                return "An account with this email already exists. Please log in instead.";
            }

            if (lower.Contains("invalid login credentials"))
            {
                return "Invalid email or password.";
            }

            if (lower.Contains("password"))
            {
                return "Your password does not meet the requirements. Use at least 8 characters with at least one letter and one number.";
            }

            if (lower.Contains("invalid email") ||
                lower.Contains("email address is invalid") ||
                lower.Contains("unable to validate email"))
            {
                return "Please enter a valid email address.";
            }

            if (lower.Contains("volunteer profile failed") ||
                lower.Contains("volunteertable") ||
                lower.Contains("volunteerpno") ||
                lower.Contains("volunteerpassword"))
            {
                return "Your login account was created, but your volunteer profile could not be saved. Please check the volunteer table fields.";
            }

            if (lower.Contains("unauthorized") || lower.Contains("401"))
            {
                return "The app could not connect to Supabase correctly. Please check the Supabase keys.";
            }

            if (lower.Contains("network") || lower.Contains("timeout"))
            {
                return "Network error. Please check your connection and try again.";
            }

            return $"Something went wrong. Details: {error}";
        }
    }
}