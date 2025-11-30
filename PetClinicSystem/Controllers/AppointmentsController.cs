using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;
using PetClinicSystem.Models;
using System;
using System.Linq;

namespace PetClinicSystem.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AppointmentsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ======== SESSION HELPERS / ROLES =========
        private int UserId => HttpContext.Session.GetInt32("UserId") ?? 0;
        private int UserRole => HttpContext.Session.GetInt32("UserRole") ?? -1;

        private bool IsAdmin => UserRole == 1;
        private bool IsStaff => UserRole == 2;
        private bool IsClient => UserRole == 0;

        // =====================================================
        // INDEX – list appointments (user-based + search)
        // =====================================================
        public IActionResult Index(string? search)
        {
            ViewBag.ActiveMenu = "Appointments";
            ViewBag.Search = search ?? string.Empty;

            var query = _db.Schedule
                .Include(s => s.Pet).ThenInclude(p => p.Owner)
                .Include(s => s.Staff)
                .AsQueryable();

            // STAFF: only appointments assigned to this staff
            if (IsStaff)
            {
                query = query.Where(s => s.StaffId == UserId);
            }

            // CLIENT: only appointments for my pets
            if (IsClient)
            {
                var myPets = _db.Pets
                    .Where(p => p.OwnerId == UserId)
                    .Select(p => p.PetId)
                    .ToList();

                query = query.Where(s => myPets.Contains(s.PetId));
            }

            // SEARCH: pet name, service, status, date (yyyy-MM-dd)
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();

                query = query.Where(s =>
                    (s.Pet != null && s.Pet.Name.ToLower().Contains(search)) ||
                    (s.Service != null && s.Service.ToLower().Contains(search)) ||
                    (s.Status != null && s.Status.ToLower().Contains(search)) ||
                    (s.ScheduleDateOld.HasValue &&
                     s.ScheduleDateOld.Value.ToString("yyyy-MM-dd").Contains(search))
                );
            }

            var list = query
                .OrderBy(s => s.ScheduleDateOld ?? DateTime.MaxValue)
                .ThenBy(s => s.ScheduleTime ?? TimeSpan.MaxValue)
                .ToList();

            return View(list);
        }

        // =====================================================
        // DETAILS – view single appointment (modal)
        // =====================================================
        public IActionResult Details(int id)
        {
            var appt = _db.Schedule
                .Include(s => s.Pet).ThenInclude(p => p.Owner)
                .Include(s => s.Staff)
                .FirstOrDefault(s => s.ScheduleId == id);

            if (appt == null)
                return NotFound();

            // STAFF: can only view own appointments
            if (IsStaff && appt.StaffId != UserId)
                return Unauthorized();

            // CLIENT: can only view their own pets' appointments
            if (IsClient && appt.Pet?.OwnerId != UserId)
                return Unauthorized();

            return PartialView("_Modal_ViewAppointment", appt);
        }

        // =====================================================
        // CREATE (GET) – open modal
        // =====================================================
        public IActionResult Create()
        {
            // Pets list
            if (IsClient)
            {
                // client: only their pets
                ViewBag.Pets = _db.Pets
                    .Include(p => p.Owner)
                    .Where(p => p.OwnerId == UserId)
                    .ToList();
            }
            else
            {
                // admin / staff: all pets
                ViewBag.Pets = _db.Pets
                    .Include(p => p.Owner)
                    .ToList();
            }

            // Staff list for dropdown (admins + staff)
            ViewBag.Staff = _db.Accounts
                .Where(a => a.IsAdmin == 1 || a.IsAdmin == 2)
                .ToList();

            return PartialView("_Modal_CreateAppointment", new Schedule());
        }

        // =====================================================
        // CREATE (POST)
        // =====================================================
        [HttpPost]
        public IActionResult Create(Schedule model)
        {
            // If client is creating, auto-assign to any staff (first active staff)
            if (IsClient)
            {
                var assignedStaffId = _db.Accounts
                    .Where(a => a.IsAdmin == 2)
                    .Select(a => a.AccountId)
                    .FirstOrDefault();

                model.StaffId = assignedStaffId;
            }

            _db.Schedule.Add(model);
            _db.SaveChanges();

            TempData["Success"] = "Appointment created successfully.";
            return RedirectToAction("Index");
        }

        // =====================================================
        // EDIT (GET) – open modal
        // =====================================================
        public IActionResult Edit(int id)
        {
            var appt = _db.Schedule
                .Include(s => s.Pet)
                .FirstOrDefault(s => s.ScheduleId == id);

            if (appt == null)
                return NotFound();

            // CLIENT: cannot edit
            if (IsClient)
                return Unauthorized();

            // STAFF: can only edit their own appointments
            if (IsStaff && appt.StaffId != UserId)
                return Unauthorized();

            // Pets list
            ViewBag.Pets = _db.Pets
                .Include(p => p.Owner)
                .ToList();

            // Staff list
            ViewBag.Staff = _db.Accounts
                .Where(a => a.IsAdmin == 1 || a.IsAdmin == 2)
                .ToList();

            return PartialView("_Modal_EditAppointment", appt);
        }

        // =====================================================
        // EDIT (POST)
        // =====================================================
        [HttpPost]
        public IActionResult Edit(Schedule model)
        {
            // CLIENT: cannot edit
            if (IsClient)
                return Unauthorized();

            var existing = _db.Schedule
                .AsNoTracking()
                .FirstOrDefault(s => s.ScheduleId == model.ScheduleId);

            if (existing == null)
                return NotFound();

            // STAFF: can only edit their own appointments
            if (IsStaff && existing.StaffId != UserId)
                return Unauthorized();

            _db.Schedule.Update(model);
            _db.SaveChanges();

            TempData["Success"] = "Appointment updated successfully!";
            return RedirectToAction("Index");
        }

        // =====================================================
        // DELETE (GET) – open confirm modal
        // =====================================================
        public IActionResult Delete(int id)
        {
            var appt = _db.Schedule
                .Include(s => s.Pet)
                .FirstOrDefault(s => s.ScheduleId == id);

            if (appt == null)
                return NotFound();

            // CLIENT: cannot delete
            if (IsClient)
                return Unauthorized();

            // STAFF: can only delete their own
            if (IsStaff && appt.StaffId != UserId)
                return Unauthorized();

            return PartialView("_Modal_DeleteAppointment", appt);
        }

        // =====================================================
        // DELETE (POST)
        // =====================================================
        [HttpPost]
        public IActionResult DeleteConfirmed(int scheduleId)
        {
            var appt = _db.Schedule.Find(scheduleId);

            if (appt == null)
                return RedirectToAction("Index");

            // CLIENT: cannot delete
            if (IsClient)
                return Unauthorized();

            // STAFF: can only delete their own
            if (IsStaff && appt.StaffId != UserId)
                return Unauthorized();

            _db.Schedule.Remove(appt);
            _db.SaveChanges();

            TempData["Success"] = "Appointment deleted successfully.";
            return RedirectToAction("Index");
        }
    }
}
