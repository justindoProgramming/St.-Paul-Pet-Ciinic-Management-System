using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;
using PetClinicSystem.Models;
using PetClinicSystem.Models.ViewModels;

namespace PetClinicSystem.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AppointmentsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // -----------------------------
        // SESSION HELPERS
        // -----------------------------
        private int? UserId => HttpContext.Session.GetInt32("UserId");
        private int? UserRole => HttpContext.Session.GetInt32("UserRole");

        // -------------------------------------------------------------
        // STATUS TRANSITION ENGINE (Clinic Workflow)
        // -------------------------------------------------------------
        private static readonly Dictionary<string, string[]> AllowedStatusTransitions = new()
{
    { "pending",   new[] { "confirmed", "urgent", "completed", "cancelled" } },
    { "confirmed", new[] { "urgent", "completed", "cancelled" } },
    { "urgent",    new[] { "confirmed", "completed", "cancelled" } },
    { "completed", Array.Empty<string>() }, // Locked
    { "cancelled", Array.Empty<string>() }  // Locked
};


        private bool CanChangeStatus(string oldStatus, string newStatus)
        {
            if (string.IsNullOrWhiteSpace(oldStatus)) return true;

            oldStatus = oldStatus.ToLower();
            newStatus = newStatus.ToLower();

            // If status didn't change, it's valid
            if (oldStatus == newStatus) return true;

            // If the old status has defined transitions
            if (AllowedStatusTransitions.TryGetValue(oldStatus, out var allowed))
                return allowed.Contains(newStatus);

            return false;
        }


        private int? GetOwnerId(int? accountId)
        {
            return _db.Owners
                .Where(o => o.AccountId == accountId)
                .Select(o => o.OwnerId)
                .FirstOrDefault();
        }

        // -----------------------------
        // LOAD DROPDOWNS
        // -----------------------------
        private void LoadDropdowns()
        {
            int? ownerId = GetOwnerId(UserId);

            ViewBag.Pets = (UserRole == 0)
                ? _db.Pets.Where(p => p.OwnerId == ownerId).Include(p => p.Owner).ToList()
                : _db.Pets.Include(p => p.Owner).ToList();

            ViewBag.Staff = _db.Accounts.Where(a => a.IsAdmin == 2).ToList();
            ViewBag.Services = _db.Service.OrderBy(s => s.Name).ToList();
        }

        // -----------------------------
        // MULTI-SLOT CONFLICT CHECKER
        // -----------------------------
        private bool HasConflict(DateTime date, int startSlotId, int blocksNeeded, int? excludeId = null)
        {
            var slots = _db.TimeSlots.OrderBy(t => t.StartTime).ToList();
            int index = slots.FindIndex(s => s.SlotId == startSlotId);
            if (index == -1) return true;

            var booked = _db.Schedule
                .Where(s => s.ScheduleDateOld == date && s.ScheduleId != excludeId)
                .Select(s => s.SlotId)
                .ToHashSet();

            for (int i = 0; i < blocksNeeded; i++)
            {
                if (index + i >= slots.Count) return true;
                if (booked.Contains(slots[index + i].SlotId)) return true;
            }

            return false;
        }

        // -----------------------------
        // INDEX
        // -----------------------------
        public IActionResult Index(string? search)
        {
            int? ownerId = GetOwnerId(UserId);

            var q = _db.Schedule
                .Include(s => s.Pet).ThenInclude(p => p.Owner)
                .Include(s => s.Staff)
                .Include(s => s.Service)
                .Include(s => s.Slot)
                .AsQueryable();

            if (UserRole == 0)
                q = q.Where(s => s.Pet.OwnerId == ownerId);

            if (UserRole == 2)
                q = q.Where(s => s.StaffId == UserId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                q = q.Where(s =>
                    s.Pet.Name.ToLower().Contains(search) ||
                    s.Service.Name.ToLower().Contains(search) ||
                    s.Status.ToLower().Contains(search)
                );
            }

            return View(q.OrderBy(s => s.ScheduleDateOld)
                         .ThenBy(s => s.Slot.StartTime)
                         .ToList());
        }

        // -----------------------------
        // CREATE (GET)
        // -----------------------------
        public IActionResult Create()
        {
            LoadDropdowns();
            return PartialView("_Modal_CreateAppointment");
        }

        // -----------------------------
        // CREATE (POST)
        // -----------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Schedule model)
        {
            if (model.ScheduleDateOld == null || model.SlotId == null)
            {
                TempData["Error"] = "Please choose a date and time.";
                return RedirectToAction("Index");
            }

            if (model.ScheduleDateOld.Value.Date < DateTime.Today)
            {
                TempData["Error"] = "You cannot book a past date.";
                return RedirectToAction("Index");
            }

            if (model.ScheduleDateOld.Value.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                TempData["Error"] = "Weekends are not available.";
                return RedirectToAction("Index");
            }

            var service = _db.Service.FirstOrDefault(s => s.ServiceId == model.ServiceId);
            if (service == null)
            {
                TempData["Error"] = "Invalid service selected.";
                return RedirectToAction("Index");
            }

            int blocksNeeded = service.DurationMinutes / 30;

            if (HasConflict(model.ScheduleDateOld.Value, model.SlotId.Value, blocksNeeded))
            {
                TempData["Error"] = "This appointment overlaps with another booking.";
                return RedirectToAction("Index");
            }

            if (UserRole == 0)
                model.Status = "Pending";

            model.ServiceName = service.Name;

            _db.Schedule.Add(model);
            _db.SaveChanges();

            TempData["Success"] = "Appointment created successfully.";
            return RedirectToAction("Index");
        }

        // -----------------------------
        // EDIT (GET)
        // -----------------------------
        public IActionResult Edit(int id)
        {
            int? ownerId = GetOwnerId(UserId);

            var appt = _db.Schedule
                .Include(s => s.Pet).ThenInclude(p => p.Owner)
                .Include(s => s.Service)
                .FirstOrDefault(s => s.ScheduleId == id);

            if (appt == null)
                return Content("Appointment not found.");

            if (UserRole == 0 && appt.Pet.OwnerId != ownerId)
                return Content("You cannot edit this appointment.");

            LoadDropdowns();
            return PartialView("_Modal_EditAppointment", appt);
        }

        // -----------------------------
        // EDIT (POST)
        // -----------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Schedule model)
        {
            var appt = _db.Schedule
                .Include(s => s.Service)
                .FirstOrDefault(s => s.ScheduleId == model.ScheduleId);

            if (appt == null)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction("Index");
            }

            // Validate status transition
            if (UserRole != 0) // staff/admin only
            {
                if (!CanChangeStatus(appt.Status ?? "pending", model.Status ?? "pending"))
                {
                    TempData["Error"] = $"You cannot change status from '{appt.Status}' to '{model.Status}'.";
                    return RedirectToAction("Index");
                }
            }

            // Validate service
            var service = _db.Service.FirstOrDefault(s => s.ServiceId == model.ServiceId);
            if (service == null)
            {
                TempData["Error"] = "Invalid service.";
                return RedirectToAction("Index");
            }

            int blocksNeeded = service.DurationMinutes / 30;

            // Check time conflicts
            if (HasConflict(model.ScheduleDateOld.Value, model.SlotId.Value, blocksNeeded, appt.ScheduleId))
            {
                TempData["Error"] = "This time slot is already taken.";
                return RedirectToAction("Index");
            }

            // Update fields
            appt.PetId = model.PetId;
            appt.StaffId = model.StaffId;
            appt.ServiceId = model.ServiceId;
            appt.ServiceName = service.Name;
            appt.ScheduleDateOld = model.ScheduleDateOld;
            appt.SlotId = model.SlotId;

            // Update status only if staff/admin
            if (UserRole != 0)
                appt.Status = model.Status;

            _db.SaveChanges();

            TempData["Success"] = "Appointment updated successfully.";
            return RedirectToAction("Index");
        }


        // -----------------------------
        // DELETE
        // -----------------------------
        public IActionResult Delete(int id)
        {
            var appt = _db.Schedule
                .Include(s => s.Pet)
                .FirstOrDefault(s => s.ScheduleId == id);

            return PartialView("_Modal_DeleteAppointment", appt);
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            var appt = _db.Schedule.Find(id);
            if (appt != null)
            {
                _db.Schedule.Remove(appt);
                _db.SaveChanges();
            }

            TempData["Success"] = "Appointment deleted.";
            return RedirectToAction("Index");
        }

        // -----------------------------
        // VIEW
        // -----------------------------
        public IActionResult ViewAppointment(int id)
        {
            var appt = _db.Schedule
                .Include(s => s.Pet).ThenInclude(p => p.Owner)
                .Include(s => s.Staff)
                .Include(s => s.Service)
                .Include(s => s.Slot)
                .FirstOrDefault(s => s.ScheduleId == id);

            return PartialView("_Modal_ViewAppointment", appt);
        }

        // -----------------------------
        // AJAX: GET VALID START TIMES
        // -----------------------------
        [HttpGet]
        public IActionResult GetValidStartTimes(DateTime date, int serviceId)
        {
            if (date < DateTime.Today) return Json(new List<object>());
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                return Json(new List<object>());

            var service = _db.Service.FirstOrDefault(s => s.ServiceId == serviceId);
            if (service == null) return Json(new List<object>());

            int blocksNeeded = service.DurationMinutes / 30;

            var slots = _db.TimeSlots.OrderBy(t => t.StartTime).ToList();

            var booked = _db.Schedule
                .Where(s => s.ScheduleDateOld == date)
                .Select(s => s.SlotId)
                .ToHashSet();

            var valid = new List<object>();

            for (int i = 0; i < slots.Count; i++)
            {
                bool ok = true;

                for (int b = 0; b < blocksNeeded; b++)
                {
                    if (i + b >= slots.Count) { ok = false; break; }
                    if (booked.Contains(slots[i + b].SlotId)) { ok = false; break; }
                }

                if (ok)
                {
                    valid.Add(new
                    {
                        slotId = slots[i].SlotId,
                        start = slots[i].StartTime.ToString(@"hh\:mm")
                    });
                }
            }

            return Json(valid);
        }

        // -----------------------------
        // AJAX: DATE RULES
        // -----------------------------
        [HttpGet]
        public IActionResult GetDateRules()
        {
            int totalSlots = _db.TimeSlots.Count();

            var rules = _db.Schedule
                .GroupBy(s => s.ScheduleDateOld)
                .Select(g => new
                {
                    date = g.Key.Value.ToString("yyyy-MM-dd"),
                    remaining = totalSlots - g.Count()
                })
                .ToList();

            return Json(rules);
        }

        public IActionResult FilterHistory(DateTime? startDate, DateTime? endDate)
        {
            var query = _db.Billing
                .Include(b => b.Pet).ThenInclude(o => o.Owner)
                .Include(b => b.Staff)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(b => b.BillingDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(b => b.BillingDate <= endDate.Value.AddDays(1));

            var list = query.ToList();

            return PartialView("_BillingHistoryPartial", list);
        }


    }
}
