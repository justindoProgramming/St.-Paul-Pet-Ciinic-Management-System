using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;
using PetClinicSystem.Models;

namespace PetClinicSystem.Controllers
{
    public class PetsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PetsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ============================
        // LIST PAGE
        // ============================
        public IActionResult Index()
        {
            ViewBag.ActiveMenu = "Pets";

            var pets = _db.Pets
                .Include(p => p.Owner)
                .ToList();

            return View(pets);
        }

        // ============================
        // CREATE (MODAL)
        // ============================
        public IActionResult Create()
        {
            ViewBag.Owners = _db.Owners.ToList();
            return PartialView("_Modal_CreatePet", new Pet());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Pet model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Owners = _db.Owners.ToList();
                return PartialView("_Modal_CreatePet", model);
            }

            // IMPORTANT FIX — remove navigation property before save
            model.Owner = null;

            _db.Pets.Add(model);
            _db.SaveChanges();

            TempData["Success"] = "Patient added successfully!";
            return RedirectToAction("Index");
        }


        // ============================
        // EDIT
        // ============================
        public IActionResult Edit(int id)
        {
            var pet = _db.Pets.Find(id);
            if (pet == null) return NotFound();

            ViewBag.Owners = _db.Owners.ToList();
            return PartialView("_Modal_EditPet", pet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Pet model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Owners = _db.Owners.ToList();
                return PartialView("_Modal_EditPet", model);
            }

            // Prevent EF navigation conflict
            model.Owner = null;

            _db.Pets.Update(model);
            _db.SaveChanges();

            TempData["Success"] = "Patient updated successfully!";
            return RedirectToAction("Index");
        }

        // ============================
        // DELETE (MODAL)
        // ============================
        public IActionResult Delete(int id)
        {
            var pet = _db.Pets
                .Include(p => p.Owner)
                .FirstOrDefault(p => p.PetId == id);

            return PartialView("_Modal_DeletePet", pet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int petId)
        {
            var pet = _db.Pets.Find(petId);
            if (pet == null) return NotFound();

            _db.Pets.Remove(pet);
            _db.SaveChanges();

            TempData["Success"] = "Patient deleted successfully!";
            return RedirectToAction("Index");
        }

        // ============================
        // VIEW (MODAL)
        // ============================
        public IActionResult Details(int id)
        {
            var pet = _db.Pets
                .Include(p => p.Owner)
                .FirstOrDefault(p => p.PetId == id);

            if (pet == null) return NotFound();

            return PartialView("_Modal_ViewPet", pet);
        }
    }
}
