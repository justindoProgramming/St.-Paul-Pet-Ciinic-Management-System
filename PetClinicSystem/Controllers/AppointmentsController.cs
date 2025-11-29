using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;
using PetClinicSystem.Models;

namespace PetClinicSystem.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AppointmentsController(ApplicationDbContext db)
        {
            _db = db;
        }

        private int UserId => HttpContext.Session.GetInt32("UserId") ?? 0;
        private int UserRole => HttpContext.Session.GetInt32("UserRole") ?? -1;

        private bool IsAdmin => UserRole == 1;
        private bool IsStaff => UserRole == 2;
        private bool IsClient => UserRole == 0;

      
        // INDEX -----------------------------------------------------
        public IActionResult Index(string? search)
        {
            ViewBag.ActiveMenu = "Appointments";
            ViewBag.Search = search ?? string.Empty;

            var query = _db.Schedule
                .Include(s => s.Pet).ThenInclude(p => p.Owner)
                .Include(s => s.Staff)
                .AsQueryable();

            // keep your role-based filters exactly as before
            if (IsStaff)
                query = query.Where(s => s.StaffId == UserId);

            if (IsClient)
            {
                var myPets = _db.Pets
                    .Where(p => p.OwnerId == UserId)
                    .Select(p => p.PetId)
                    .ToList();

                query = query.Where(s => myPets.Contains(s.PetId));
            }

            // SEARCH FILTER (pet name, service, status, date)
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


        // DETAILS ---------------------------------------------------
        public IActionResult Details(int id)
        {
            var appt = _db.Schedule
                .Include(s => s.Pet).ThenInclude(p => p.Owner)
                .Include(s => s.Staff)
                .FirstOrDefault(s => s.ScheduleId == id);

            if (appt == null)
                return NotFound();

            if (IsStaff && appt.StaffId != UserId)
                return Unauthorized();

            if (IsClient && appt.Pet.OwnerId != UserId)
                return Unauthorized();

            return PartialView("_Modal_ViewAppointment", appt);
        }

        // CREATE MODAL (GET) ----------------------------------------
        public IActionResult Create()
        {
            ViewBag.Pets = _db.Pets.Include(p => p.Owner).ToList();
            ViewBag.Staff = _db.Accounts.Where(a => a.IsAdmin == 1 || a.IsAdmin == 2).ToList();

            return PartialView("_Modal_CreateAppointment", new Schedule());
        }

        // CREATE (POST) --------------------------------------------
        [HttpPost]
        public IActionResult Create(Schedule model)
        {
            if (IsClient)
            {
                model.StaffId = _db.Accounts
                    .Where(a => a.IsAdmin == 2)
                    .Select(a => a.AccountId)
                    .FirstOrDefault();
            }

            _db.Schedule.Add(model);
            _db.SaveChanges();

            TempData["Success"] = "Appointment created successfully";

            return RedirectToAction("Index");
        }


        // EDIT MODAL (GET) ------------------------------------------
        public IActionResult Edit(int id)
        {
            var appt = _db.Schedule
                .Include(s => s.Pet)
                .FirstOrDefault(s => s.ScheduleId == id);

            if (appt == null)
                return NotFound();

            ViewBag.Pets = _db.Pets.Include(p => p.Owner).ToList();
            ViewBag.Staff = _db.Accounts.Where(a => a.IsAdmin == 1 || a.IsAdmin == 2).ToList();

            return PartialView("_Modal_EditAppointment", appt);
        }

        // EDIT (POST) -----------------------------------------------
        [HttpPost]
        public IActionResult Edit(Schedule model)
        {
            if (IsClient)
                return Unauthorized();

            _db.Schedule.Update(model);
            _db.SaveChanges();

            TempData["Success"] = "Appointment updated successfully!";
            return RedirectToAction("Index");
        }


        // DELETE MODAL (GET) ----------------------------------------
        public IActionResult Delete(int id)
        {
            var appt = _db.Schedule
                .Include(s => s.Pet)
                .FirstOrDefault(s => s.ScheduleId == id);

            return PartialView("_Modal_DeleteAppointment", appt);
        }

        // DELETE (POST) ---------------------------------------------
        [HttpPost]
        public IActionResult DeleteConfirmed(int scheduleId)
        {
            var appt = _db.Schedule.Find(scheduleId);

            if (appt != null)
            {
                _db.Schedule.Remove(appt);
                _db.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}
