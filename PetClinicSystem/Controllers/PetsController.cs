using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;
using PetClinicSystem.Models;
using System;
using System.Linq;

namespace PetClinicSystem.Controllers
{
    public class PetsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PetsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ========= SESSION / ROLE HELPERS =========
        private int UserId => HttpContext.Session.GetInt32("UserId") ?? 0;
        private int UserRole => HttpContext.Session.GetInt32("UserRole") ?? -1;

        private bool IsAdmin => UserRole == 1;
        private bool IsStaff => UserRole == 2;
        private bool IsClient => UserRole == 0;

        // =========================================
        //  INDEX – LIST ALL PETS (USER-BASED)
        // =========================================
        public IActionResult Index()
        {
            ViewBag.ActiveMenu = "Pets";

            var query = _db.Pets
                .Include(p => p.Owner)
                .AsQueryable();

            // Clients: only see their own pets (OwnerId == logged-in AccountId)
            if (IsClient && UserId != 0)
            {
                query = query.Where(p => p.OwnerId == UserId);
            }

            // Admin & Staff: see all pets
            var pets = query
                .OrderBy(p => p.Name)
                .ToList();

            return View(pets);
        }

        // =========================================
        //  CREATE PET (GET) – OPEN MODAL
        // =========================================
        [HttpGet]
        public IActionResult Create()
        {
            // list of owners for dropdown
            ViewBag.Owners = _db.Owners
                .OrderBy(o => o.FullName)
                .ToList();

            var model = new Pet
            {
                BirthDate = DateTime.Today
            };

            return PartialView("_Modal_CreatePet", model);
        }

        // =========================================
        //  CREATE PET (POST)
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Pet model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Owners = _db.Owners
                    .OrderBy(o => o.FullName)
                    .ToList();

                return PartialView("_Modal_CreatePet", model);
            }

            // avoid EF trying to insert/update Owner again
            model.Owner = null;

            _db.Pets.Add(model);
            _db.SaveChanges();

            TempData["Success"] = "Patient added successfully!";
            return RedirectToAction("Index");
        }

        // =========================================
        //  EDIT PET (GET) – OPEN MODAL
        // =========================================
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var pet = _db.Pets
                .Include(p => p.Owner)
                .FirstOrDefault(p => p.PetId == id);

            if (pet == null)
                return NotFound();

            ViewBag.Owners = _db.Owners
                .OrderBy(o => o.FullName)
                .ToList();

            return PartialView("_Modal_EditPet", pet);
        }

        // =========================================
        //  EDIT PET (POST)
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Pet model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Owners = _db.Owners
                    .OrderBy(o => o.FullName)
                    .ToList();

                return PartialView("_Modal_EditPet", model);
            }

            // detach navigation so EF doesn’t try to re-attach Owner
            model.Owner = null;

            _db.Pets.Update(model);
            _db.SaveChanges();

            TempData["Success"] = "Patient updated successfully!";
            return RedirectToAction("Index");
        }

        // =========================================
        //  DELETE PET (GET) – CONFIRM MODAL
        // =========================================
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var pet = _db.Pets
                .Include(p => p.Owner)
                .FirstOrDefault(p => p.PetId == id);

            if (pet == null)
                return NotFound();

            return PartialView("_Modal_DeletePet", pet);
        }

        // =========================================
        //  DELETE PET (POST)
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var pet = _db.Pets.Find(id);
            if (pet == null)
                return NotFound();

            // Example checks – tweak based on your DbSets
            bool hasAppointments = _db.Schedule.Any(s => s.PetId == id);
            bool hasMedical = _db.MedicalRecords.Any(m => m.PetId == id);

            if (hasAppointments || hasMedical)
            {
                TempData["Error"] = "This patient has existing records and cannot be deleted.";
                return RedirectToAction("Index");
            }

            _db.Pets.Remove(pet);
            _db.SaveChanges();

            TempData["Success"] = "Patient deleted successfully!";
            return RedirectToAction("Index");
        }


        // =========================================
        //  VIEW PET (GET) – DETAILS MODAL
        // =========================================
        [HttpGet]
        public IActionResult Details(int id)
        {
            var pet = _db.Pets
                .Include(p => p.Owner)
                .FirstOrDefault(p => p.PetId == id);

            if (pet == null)
                return NotFound();

            return PartialView("_Modal_ViewPet", pet);
        }
    }
}
