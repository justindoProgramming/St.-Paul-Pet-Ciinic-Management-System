using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;
using PetClinicSystem.Models;
using System;
using System.Linq;

namespace PetClinicSystem.Controllers
{
    public class OwnersController : Controller
    {
        private readonly ApplicationDbContext _db;

        public OwnersController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ===================== INDEX =====================
        public IActionResult Index()
        {
            ViewBag.ActiveMenu = "Owners";

            var owners = _db.Owners
                .Include(o => o.Pets)
                .OrderBy(o => o.FullName)
                .ToList();

            return View(owners);
        }

        // ===================== CREATE =====================
        [HttpGet]
        public IActionResult CreateOwner()
        {
            var model = new Owner();
            return PartialView("_Modal_CreateOwner", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateOwner(Owner model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid owner data.");
            }

            _db.Owners.Add(model);
            _db.SaveChanges();

            return Ok(new { message = "Owner created successfully." });
        }

        // ===================== VIEW =====================
        [HttpGet]
        public IActionResult ViewOwner(int id)
        {
            var owner = _db.Owners
                .Include(o => o.Pets)
                .FirstOrDefault(o => o.OwnerId == id);

            if (owner == null) return NotFound();

            return PartialView("_Modal_ViewOwner", owner);
        }

        // ===================== EDIT =====================
        [HttpGet]
        public IActionResult EditOwner(int id)
        {
            var owner = _db.Owners.FirstOrDefault(o => o.OwnerId == id);
            if (owner == null) return NotFound();

            return PartialView("_Modal_EditOwner", owner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditOwner(Owner model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid owner data.");
            }

            var owner = _db.Owners.FirstOrDefault(o => o.OwnerId == model.OwnerId);
            if (owner == null) return NotFound();

            owner.FullName = model.FullName;
            owner.PhoneNumber = model.PhoneNumber;
            owner.Email = model.Email;
            owner.Address = model.Address;
            owner.EmergencyContact1 = model.EmergencyContact1;
            owner.EmergencyContact2 = model.EmergencyContact2;

            _db.Owners.Update(owner);
            _db.SaveChanges();

            return Ok(new { message = "Owner updated successfully." });
        }

        // ===================== DELETE =====================
        [HttpGet]
        public IActionResult DeleteOwner(int id)
        {
            var owner = _db.Owners
                .Include(o => o.Pets)
                .FirstOrDefault(o => o.OwnerId == id);

            if (owner == null) return NotFound();

            return PartialView("_Modal_DeleteOwner", owner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteOwnerConfirmed(int id)
        {
            var owner = _db.Owners
                .Include(o => o.Pets)
                .FirstOrDefault(o => o.OwnerId == id);

            if (owner != null)
            {
                // optional: block delete if owner still has pets
                if (owner.Pets != null && owner.Pets.Any())
                {
                    return BadRequest("Cannot delete owner with existing patients.");
                }

                _db.Owners.Remove(owner);
                _db.SaveChanges();
            }

            return Ok(new { message = "Owner deleted." });
        }
    }
}
