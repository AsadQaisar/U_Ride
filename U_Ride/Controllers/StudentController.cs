using Microsoft.AspNetCore.Mvc;

namespace U_Ride.Controllers
{
    public class StudentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
