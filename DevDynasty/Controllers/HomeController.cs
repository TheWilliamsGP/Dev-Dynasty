using Microsoft.AspNetCore.Mvc;

namespace DevDynasty.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Friends of Valkenberg Trust — Supporting Mental Health Recovery";
            ViewData["Description"] = "An NPO working alongside Valkenberg Psychiatric Hospital staff to promote recovery for people with serious mental illnesses.";
            return View();
        }

        public IActionResult About()
        {
            ViewData["Title"] = "About — Friends of Valkenberg Trust";
            ViewData["Description"] = "Learn about the Friends of Valkenberg Trust, our history, mission and the people we serve.";
            return View();
        }

        public IActionResult Community()
        {
            ViewData["Title"] = "Community — Friends of Valkenberg Trust";
            ViewData["Description"] = "Stories and updates from our community.";
            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Title"] = "Contact Us — Friends of Valkenberg Trust";
            ViewData["Description"] = "Get in touch with the Friends of Valkenberg Trust.";
            return View();
        }

        public IActionResult Donate()
        {
            ViewData["Title"] = "Donate — Friends of Valkenberg Trust";
            ViewData["Description"] = "Make a donation to support mental health recovery.";
            return View();
        }

        public IActionResult Donations()
        {
            ViewData["Title"] = "Donations — Friends of Valkenberg Trust";
            ViewData["Description"] = "Support our work by donating to the Friends of Valkenberg Trust.";
            return View();
        }

        public IActionResult Events()
        {
            ViewData["Title"] = "Events — Friends of Valkenberg Trust";
            ViewData["Description"] = "Upcoming events, fundraisers and activities at Friends of Valkenberg Trust.";
            return View();
        }

        public IActionResult Resources()
        {
            ViewData["Title"] = "Resources — Friends of Valkenberg Trust";
            ViewData["Description"] = "Mental health resources and information.";
            return View();
        }

        public IActionResult Services()
        {
            ViewData["Title"] = "Services — Friends of Valkenberg Trust";
            ViewData["Description"] = "How our 60+ volunteers support patients at Valkenberg Psychiatric Hospital.";
            return View();
        }

        public IActionResult Volunteer()
        {
            ViewData["Title"] = "Volunteer — Friends of Valkenberg Trust";
            ViewData["Description"] = "Become a volunteer with the Friends of Valkenberg Trust.";
            return View();
        }

        [Route("404")]
        public IActionResult NotFoundPage()
        {
            ViewData["Title"] = "Page not found";
            return View("NotFound");
        }

        [Route("Error")]
        public IActionResult Error()
        {
            ViewData["Title"] = "Something went wrong";
            return View();
        }
    }
}