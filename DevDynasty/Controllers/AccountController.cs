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

                if (!result.IsSuccess || !result.UserId.HasValue)
                {
                    ModelState.AddModelError("", result.ErrorMessage ?? "Registration failed. Please try again.");
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Registration failed. Please try again.";
                    return View(model);
                }

                SaveUserSession(result);

                TempData["SuccessMessage"] = "Registration successful. Welcome!";

                return RedirectToAction("Dashboard", "VolunteerDashboard", new
                {
                    volunteerId = result.UserId.Value
                });
            }
            catch (InvalidOperationException ex)
            {
                var message = FriendlyAuthError(ex.Message);

                ModelState.AddModelError("", message);
                TempData["ErrorMessage"] = message;

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

                if (!result.IsSuccess || !result.UserId.HasValue)
                {
                    ModelState.AddModelError("", result.ErrorMessage ?? "Invalid email or password.");
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Invalid email or password.";
                    return View(model);
                }

                SaveUserSession(result);

                TempData["SuccessMessage"] = "Login successful.";

                if (result.IsAdmin)
                {
                    return RedirectToAction("Dashboard", "Admin");
                }

                return RedirectToAction("Dashboard", "VolunteerDashboard", new
                {
                    volunteerId = result.UserId.Value
                });
            }
            catch (InvalidOperationException ex)
            {
                var message = FriendlyAuthError(ex.Message);

                ModelState.AddModelError("", message);
                TempData["ErrorMessage"] = message;

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
            Response.Cookies.Delete("AdminId");
            Response.Cookies.Delete("UserRole");
            Response.Cookies.Delete("AccessToken");
            Response.Cookies.Delete("VolunteerAccessToken");

            TempData["SuccessMessage"] = "You have been logged out.";

            return RedirectToAction("Index", "Home");
        }

        private void SaveUserSession(SupabaseVolunteerAuthService.AuthResult result)
        {
            if (!result.UserId.HasValue)
                return;

            HttpContext.Session.SetString("UserRole", result.IsAdmin ? "Admin" : "Volunteer");

            Response.Cookies.Append("UserRole", result.IsAdmin ? "Admin" : "Volunteer", new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });

            if (result.IsAdmin)
            {
                HttpContext.Session.SetString("AdminId", result.UserId.Value.ToString());

                Response.Cookies.Append("AdminId", result.UserId.Value.ToString(), new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddHours(8)
                });
            }
            else
            {
                HttpContext.Session.SetString("VolunteerId", result.UserId.Value.ToString());

                Response.Cookies.Append("VolunteerId", result.UserId.Value.ToString(), new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddHours(8)
                });
            }

            if (!string.IsNullOrWhiteSpace(result.AccessToken))
            {
                Response.Cookies.Append("AccessToken", result.AccessToken, new CookieOptions
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

            if (lower.Contains("over_email_send_rate_limit") ||
                lower.Contains("email rate limit exceeded") ||
                lower.Contains("too many requests") ||
                lower.Contains("429"))
            {
                return "Too many signup attempts were made. Please wait a while before trying again.";
            }

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

            if (lower.Contains("admin lookup failed"))
            {
                return "The app could not check admin access. Please check the admin table setup.";
            }

            if (lower.Contains("volunteer lookup failed"))
            {
                return "The app could not check volunteer access. Please check the volunteer table setup.";
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
                return "The app could not connect to Supabase correctly. Please check the Supabase service role key.";
            }

            if (lower.Contains("network") || lower.Contains("timeout"))
            {
                return "Network error. Please check your connection and try again.";
            }

            return $"Something went wrong. Details: {error}";
        }
    }
}