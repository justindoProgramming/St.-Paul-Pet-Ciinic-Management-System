using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using PetClinicSystem.Data;
using PetClinicSystem.Models;
using PetClinicSystem.Models.ViewModels;
using System.Linq;

namespace PetClinicSystem.Controllers
{
    public class ClientRecordsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ClientRecordsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            if (userId == 0)
                return RedirectToAction("Index", "Home");

            // ============================
            // PETS OWNED BY CLIENT
            // ============================
            var pets = _db.Pets
                .Include(p => p.Owner)
                .Where(p => p.OwnerId == userId)
                .ToList();

            // ============================
            // MEDICAL RECORDS
            // ============================
            var medical = _db.MedicalRecords
                .Include(m => m.Pet)
                .Include(m => m.Staff)
                .Include(m => m.Prescription)
                .Include(m => m.Vaccination)
                .Where(m => m.Pet.OwnerId == userId)
                .OrderByDescending(m => m.Date)
                .ToList();

            // ============================
            // PRESCRIPTIONS
            // ============================
            var prescriptions = _db.Prescriptions
                .Include(p => p.Pet)
                .Include(p => p.Staff)
                .Where(p => p.Pet.OwnerId == userId)
                .OrderByDescending(p => p.Date)
                .ToList();

            // ============================
            // VACCINATIONS
            // ============================
            var vaccinations = _db.Vaccinations
                .Include(v => v.Pet)
                .Include(v => v.Staff)
                .Where(v => v.Pet.OwnerId == userId)
                .OrderByDescending(v => v.DateGiven)
                .ToList();

            // ============================
            // BUILD VIEW MODEL
            // ============================
            var model = new ClientRecordsViewModel
            {
                Pets = pets,
                MedicalRecords = medical,
                Prescriptions = prescriptions,
                Vaccinations = vaccinations
            };

            ViewBag.ActiveMenu = "Records";

            return View(model);
        }
    }
}
