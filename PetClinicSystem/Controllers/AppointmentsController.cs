using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using PetClinicSystem.Data;
using PetClinicSystem.Models;
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

        // 0 = Client, 1 = Admin, 2 = Staff
        private int? CurrentUserId => HttpContext.Session.GetInt32("UserId");
        private int? CurrentRole   => HttpContext.Session.GetInt32("UserRole");

        // LOAD DROPDOWN DATA (pets filtered for clients)
        private void LoadDropdowns()
        {
            var role   = CurrentRole;
            var userId = CurrentUserId;

            var petsQuery = _db.Pets
                .Include(p => p.Owner)
                .AsQueryable();

            // client: only their pets in dropdown
            if (role == 0 && userId.HasValue)
            {
                petsQuery = petsQuery.Where(p => p.OwnerId == userId.Value);
            }

            ViewBag.Pets     = petsQuery.ToList();
            ViewBag.Staff    = _db.Accounts.Where(a => a.IsAdmin == 2).ToList();
            ViewBag.Services = _db.Service.ToList();
        }

        // INDEX
        public IActionResult Index(string? search)
        {
            ViewBag.ActiveMenu = "Appointments";

            var role   = CurrentRole;
            var userId = CurrentUserId;

            var query = _db.Schedule
                .Include(s => s.Pet).ThenInclude(o => o.Owner)
                .Include(s => s.Staff)
                .Include(s => s.Service)
                .AsQueryable();

            // CLIENT → only their appointments
            if (role == 0 && userId.HasValue)
            {
                query = query.Where(s => s.Pet.OwnerId == userId.Value);
            }
            // STAFF → (optional) only their assigned schedules
            else if (role == 2 && userId.HasValue)
            {
                query = query.Where(s => s.StaffId == userId.Value);
            }
            // ADMIN → all

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(s =>
                    s.Pet.Name.ToLower().Contains(search) ||
                    (s.Status != null && s.Status.ToLower().Contains(search)) ||
                    (s.Service != null && s.Service.Name.ToLower().Contains(search)));
            }

            // optional: order by date/time
            query = query
                .OrderBy(s => s.ScheduleDateOld)
                .ThenBy(s => s.ScheduleTime);

            return View(query.ToList());
        }

        // CREATE (MODAL)
        public IActionResult Create()
        {
            LoadDropdowns();
            return PartialView("_Modal_CreateAppointment");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Schedule model)
        {
            var role   = CurrentRole;
            var userId = CurrentUserId;

            // client can only book for their own pets
            if (role == 0 && userId.HasValue)
            {
                bool ownsPet = _db.Pets.Any(p => p.PetId == model.PetId &&
                                                 p.OwnerId == userId.Value);
                if (!ownsPet)
                {
                    TempData["Error"] = "You can only book appointments for your own pets.";
                    return RedirectToAction("Index");
                }

                // client is not allowed to control Status
                model.Status = "Pending";
            }

            // set legacy service name
            if (model.ServiceId != null)
            {
                var svc = _db.Service.Find(model.ServiceId.Value);
                model.ServiceName = svc?.Name;
            }

            _db.Schedule.Add(model);
            _db.SaveChanges();
            TempData["Success"] = "Appointment created.";

            return RedirectToAction("Index");
        }

        // EDIT (MODAL)
        public IActionResult Edit(int id)
        {
            var role   = CurrentRole;
            var userId = CurrentUserId;

            Schedule? schedule;

            if (role == 0 && userId.HasValue)
            {
                // client: only their own appointment
                schedule = _db.Schedule
                    .Include(s => s.Pet).ThenInclude(o => o.Owner)
                    .Include(s => s.Staff)
                    .Include(s => s.Service)
                    .FirstOrDefault(s => s.ScheduleId == id &&
                                         s.Pet.OwnerId == userId.Value);
            }
            else
            {
                // staff/admin
                schedule = _db.Schedule
                    .Include(s => s.Pet).ThenInclude(o => o.Owner)
                    .Include(s => s.Staff)
                    .Include(s => s.Service)
                    .FirstOrDefault(s => s.ScheduleId == id);
            }

            if (schedule == null)
                return Content("Appointment not found");

            LoadDropdowns();
            return PartialView("_Modal_EditAppointment", schedule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Schedule model)
        {
            var role   = CurrentRole;
            var userId = CurrentUserId;

            var schedule = _db.Schedule
                .Include(s => s.Pet).ThenInclude(o => o.Owner)
                .FirstOrDefault(s => s.ScheduleId == model.ScheduleId);

            if (schedule == null)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction("Index");
            }

            // client can only edit their own appointment
            if (role == 0 && userId.HasValue &&
                schedule.Pet.OwnerId != userId.Value)
            {
                TempData["Error"] = "You are not allowed to edit this appointment.";
                return RedirectToAction("Index");
            }

            // update allowed fields
            schedule.PetId           = model.PetId;
            schedule.StaffId         = model.StaffId;
            schedule.ServiceId       = model.ServiceId;
            schedule.ScheduleDateOld = model.ScheduleDateOld;
            schedule.ScheduleTime    = model.ScheduleTime;

            // update ServiceName
            if (model.ServiceId != null)
            {
                var svc = _db.Service.Find(model.ServiceId.Value);
                schedule.ServiceName = svc?.Name;
            }
            else
            {
                schedule.ServiceName = null;
            }

            // ONLY staff/admin can change Status
            if (role != 0)
            {
                schedule.Status = model.Status;
            }

            _db.SaveChanges();
            TempData["Success"] = "Appointment updated.";

            return RedirectToAction("Index");
        }

        // VIEW
        public IActionResult ViewAppointment(int id)
        {
            var role   = CurrentRole;
            var userId = CurrentUserId;

            var schedule = _db.Schedule
                .Include(s => s.Pet).ThenInclude(o => o.Owner)
                .Include(s => s.Staff)
                .Include(s => s.Service)
                .FirstOrDefault(s => s.ScheduleId == id);

            if (schedule == null)
                return Content("Appointment not found");

            // client can only view their own appointment
            if (role == 0 && userId.HasValue &&
                schedule.Pet.OwnerId != userId.Value)
            {
                return Content("Appointment not found");
            }

            return PartialView("_Modal_ViewAppointment", schedule);
        }

        // DELETE
        public IActionResult Delete(int id)
        {
            var role   = CurrentRole;
            var userId = CurrentUserId;

            var schedule = _db.Schedule
                .Include(s => s.Pet).ThenInclude(o => o.Owner)
                .FirstOrDefault(s => s.ScheduleId == id);

            if (schedule == null)
                return Content("Appointment not found");

            // client can only delete their own appointment (if you allow)
            if (role == 0 && userId.HasValue &&
                schedule.Pet.OwnerId != userId.Value)
            {
                return Content("Appointment not found");
            }

            return PartialView("_Modal_DeleteAppointment", schedule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var role   = CurrentRole;
            var userId = CurrentUserId;

            var schedule = _db.Schedule
                .Include(s => s.Pet).ThenInclude(o => o.Owner)
                .FirstOrDefault(s => s.ScheduleId == id);

            if (schedule == null)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction("Index");
            }

            if (role == 0 && userId.HasValue &&
                schedule.Pet.OwnerId != userId.Value)
            {
                TempData["Error"] = "You are not allowed to delete this appointment.";
                return RedirectToAction("Index");
            }

            _db.Schedule.Remove(schedule);
            _db.SaveChanges();

            TempData["Success"] = "Appointment deleted.";
            return RedirectToAction("Index");
        }
    }
}
