using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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

        // ======== SESSION / ROLE HELPERS ========
        private int UserId => HttpContext.Session.GetInt32("UserId") ?? 0;
        private int UserRole => HttpContext.Session.GetInt32("UserRole") ?? -1;

        private bool IsAdmin => UserRole == 1;
        private bool IsStaff => UserRole == 2;
        private bool IsClient => UserRole == 0;

        // ===================== INDEX =====================
        // /Owners/Index?search=
        public IActionResult Index(string? search)
        {
            ViewBag.ActiveMenu = "Owners";
            ViewBag.Search = search ?? string.Empty;

            var query = _db.Owners
                .Include(o => o.Pets)
                .AsQueryable();

            // (Optional) you COULD restrict clients to only their own owner record
            // but there is no direct Account ↔ Owner mapping in the model,
            // so we leave all owners visible for now.
            // Admin + Staff see all owners.

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();

                query = query.Where(o =>
                    o.FullName.ToLower().Contains(search) ||
                    o.PhoneNumber.ToLower().Contains(search) ||
                    o.Email.ToLower().Contains(search) ||
                    o.Address.ToLower().Contains(search));
            }

            var owners = query
                .OrderBy(o => o.FullName)
                .ToList();

            return View(owners);
        }

        // ===================== CREATE =====================
        [HttpGet]
        public IActionResult CreateOwner()
        {
            // Clients are not allowed to create owners
            if (IsClient)
                return Unauthorized();

            var model = new Owner();
            return PartialView("_Modal_CreateOwner", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateOwner(Owner model)
        {
            if (IsClient)
                return Unauthorized();

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

            if (owner == null)
                return NotFound();

            // (If you later link Owner ↔ Account, you can restrict clients here)
            return PartialView("_Modal_ViewOwner", owner);
        }

        // ===================== EDIT =====================
        [HttpGet]
        public IActionResult EditOwner(int id)
        {
            if (IsClient)
                return Unauthorized();

            var owner = _db.Owners.FirstOrDefault(o => o.OwnerId == id);
            if (owner == null)
                return NotFound();

            return PartialView("_Modal_EditOwner", owner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditOwner(Owner model)
        {
            if (IsClient)
                return Unauthorized();

            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid owner data.");
            }

            var owner = _db.Owners.FirstOrDefault(o => o.OwnerId == model.OwnerId);
            if (owner == null)
                return NotFound();

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
            if (IsClient)
                return Unauthorized();

            var owner = _db.Owners
                .Include(o => o.Pets)
                .FirstOrDefault(o => o.OwnerId == id);

            if (owner == null)
                return NotFound();

            return PartialView("_Modal_DeleteOwner", owner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteOwnerConfirmed(int id)
        {
            if (IsClient)
                return Unauthorized();

            var owner = _db.Owners
                .Include(o => o.Pets)
                .FirstOrDefault(o => o.OwnerId == id);

            if (owner != null)
            {
                // Block delete if owner still has pets
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
