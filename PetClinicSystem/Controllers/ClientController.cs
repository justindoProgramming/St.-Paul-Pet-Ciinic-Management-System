using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;

namespace PetClinicSystem.Controllers
{
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ClientController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var clientId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetInt32("UserRole");

            if (clientId == null || role != 0)
                return RedirectToAction("Index", "Home");

            ViewBag.Layout = "_Layout_Client";
            ViewBag.ActiveMenu = "Dashboard";

            // PET LIST
            ViewBag.MyPets = _db.Pets
                .Include(p => p.Owner)
                .Where(p => p.OwnerId == clientId)
                .ToList();

            // UPCOMING APPOINTMENTS
            var today = DateTime.Today;
            ViewBag.Upcoming = _db.Schedule
                .Include(s => s.Pet)
                .Where(s => s.Pet.OwnerId == clientId && s.ScheduleDateOld >= today)
                .OrderBy(s => s.ScheduleDateOld)
                .Take(5)
                .ToList();

            // VACCINATION RECORDS
            ViewBag.Vaccines = _db.Vaccinations
                .Include(v => v.Pet)
                .Where(v => v.Pet.OwnerId == clientId)
                .OrderBy(v => v.NextDueDate)
                .Take(5)
                .ToList();

            return View();
        }
    }
}
