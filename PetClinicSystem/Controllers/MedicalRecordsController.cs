using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;
using PetClinicSystem.Models;
using System;
using System.Linq;

namespace PetClinicSystem.Controllers
{
    public class MedicalRecordsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public MedicalRecordsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ========= MAIN PAGE =========
        public IActionResult Index()
        {
            ViewBag.ActiveMenu = "Records";

            var list = _db.MedicalRecords
                .Include(r => r.Pet).ThenInclude(p => p.Owner)
                .Include(r => r.Staff)
                .OrderByDescending(r => r.Date)
                .ToList();

            return View(list); // Views/MedicalRecords/Index.cshtml
        }

        // LIST PARTIAL (for AJAX refresh)
        public IActionResult RecordList()
        {
            var list = _db.MedicalRecords
                .Include(r => r.Pet).ThenInclude(p => p.Owner)
                .Include(r => r.Staff)
                .OrderByDescending(r => r.Date)
                .ToList();

            return PartialView("_MedicalRecords", list);
        }

        // ========= CREATE =========
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Pets = _db.Pets
                .Include(p => p.Owner)
                .OrderBy(p => p.Name)
                .ToList();

            ViewBag.Staff = _db.Accounts
                .Where(a => a.IsAdmin == 1 || a.IsAdmin == 2) // admin or staff
                .OrderBy(a => a.FullName)
                .ToList();

            var model = new MedicalRecord
            {
                Date = DateTime.Today
            };

            return PartialView("_Modal_CreateMedicalRecord", model);
        }

        [HttpPost]
        public IActionResult Create(MedicalRecord model)
        {
            // simple manual validation
            if (model.PetId <= 0 || model.StaffId <= 0)
            {
                return BadRequest("Please select a patient and a staff/doctor.");
            }

            if (string.IsNullOrWhiteSpace(model.Description) &&
                string.IsNullOrWhiteSpace(model.Diagnosis) &&
                string.IsNullOrWhiteSpace(model.Treatment))
            {
                return BadRequest("Please enter at least one field (description, diagnosis, or treatment).");
            }

            if (!model.Date.HasValue)
            {
                model.Date = DateTime.Today;
            }

            _db.MedicalRecords.Add(model);
            _db.SaveChanges();

            return Ok(new { message = "Medical record created." });
        }

        // ========= EDIT =========
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var record = _db.MedicalRecords
                .FirstOrDefault(r => r.RecordId == id);

            if (record == null)
                return NotFound();

            ViewBag.Pets = _db.Pets
                .Include(p => p.Owner)
                .OrderBy(p => p.Name)
                .ToList();

            ViewBag.Staff = _db.Accounts
                .Where(a => a.IsAdmin == 1 || a.IsAdmin == 2)
                .OrderBy(a => a.FullName)
                .ToList();

            return PartialView("_Modal_EditMedicalRecord", record);
        }

        [HttpPost]
        public IActionResult Edit(MedicalRecord model)
        {
            var record = _db.MedicalRecords
                .FirstOrDefault(r => r.RecordId == model.RecordId);

            if (record == null)
                return NotFound();

            if (model.PetId <= 0 || model.StaffId <= 0)
            {
                return BadRequest("Please select a patient and a staff/doctor.");
            }

            if (!model.Date.HasValue)
            {
                model.Date = DateTime.Today;
            }

            record.PetId = model.PetId;
            record.StaffId = model.StaffId;
            record.Description = model.Description;
            record.Diagnosis = model.Diagnosis;
            record.Treatment = model.Treatment;
            record.Date = model.Date;

            _db.SaveChanges();

            return Ok(new { message = "Medical record updated." });
        }

        // ========= DELETE =========
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var record = _db.MedicalRecords
                .Include(r => r.Pet).ThenInclude(p => p.Owner)
                .Include(r => r.Staff)
                .FirstOrDefault(r => r.RecordId == id);

            if (record == null)
                return NotFound();

            return PartialView("_Modal_DeleteMedicalRecord", record);
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            var record = _db.MedicalRecords
                .FirstOrDefault(r => r.RecordId == id);

            if (record != null)
            {
                _db.MedicalRecords.Remove(record);
                _db.SaveChanges();
            }

            return Ok(new { message = "Medical record deleted." });
        }

    }
}

