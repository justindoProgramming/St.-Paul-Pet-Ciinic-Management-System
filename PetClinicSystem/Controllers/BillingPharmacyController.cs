using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetClinicSystem.Data;
using PetClinicSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PetClinicSystem.Controllers
{
    public class BillingPharmacyController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BillingPharmacyController(ApplicationDbContext db)
        {
            _db = db;
        }

        // =========================================
        //  MAIN PAGE (POS + TABS)
        // =========================================
        public IActionResult Index()
        {
            ViewBag.ActiveMenu = "Billing";

            // POS
            ViewBag.Pets = _db.Pets
                .Include(p => p.Owner)
                .ToList();

            // Prescriptions
            ViewBag.Prescriptions = _db.Prescriptions
                .Include(p => p.Pet).ThenInclude(o => o.Owner)
                .Include(p => p.Staff)
                .OrderByDescending(p => p.Date)
                .ToList();

            // Drugs
            ViewBag.Drugs = _db.Drugs
                .OrderBy(d => d.DrugName)
                .ToList();

            // Vaccinations
            ViewBag.Vaccinations = _db.Vaccinations
                .Include(v => v.Pet).ThenInclude(o => o.Owner)
                .Include(v => v.Staff)
                .OrderByDescending(v => v.DateGiven)
                .ToList();

            return View();
        }

        // =========================================
        //  POS — SAVE TRANSACTION
        // =========================================
        [HttpPost]
        public IActionResult SaveTransaction([FromBody] List<Billing> items)
        {
            if (items == null || items.Count == 0)
                return BadRequest("Empty cart!");

            foreach (var item in items)
            {
                _db.Billing.Add(item);
            }

            _db.SaveChanges();

            return Ok(new { message = "Payment saved successfully!" });
        }

        // =========================================
        //  PRESCRIPTIONS — LIST (AJAX)
        // =========================================
        public IActionResult PrescriptionList()
        {
            var list = _db.Prescriptions
                .Include(p => p.Pet).ThenInclude(o => o.Owner)
                .Include(p => p.Staff)
                .OrderByDescending(p => p.Date)
                .ToList();

            return PartialView("_Prescriptions", list);
        }

        // =========================================
        //  PRESCRIPTIONS — CREATE
        // =========================================
        [HttpGet]
        public IActionResult CreatePrescription()
        {
            ViewBag.Pets = _db.Pets
                .Include(p => p.Owner)
                .ToList();

            ViewBag.Staff = _db.Accounts
                .Where(a => a.IsAdmin == 2 || a.IsAdmin == 1)
                .ToList();

            ViewBag.Drugs = _db.Drugs
                .OrderBy(d => d.DrugName)
                .ToList();

            var model = new Prescription
            {
                Date = DateTime.Today
            };

            return PartialView("_Modal_CreatePrescription", model);
        }

        [HttpPost]
        public IActionResult CreatePrescription(Prescription model)
        {
            // 1️⃣ Validate model
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                var joined = string.Join("; ", errors);
                return BadRequest("Invalid prescription data. Details: " + joined);
            }

            using var transaction = _db.Database.BeginTransaction();

            try
            {
                // 2️⃣ Save prescription
                _db.Prescriptions.Add(model);
                _db.SaveChanges(); // fills model.PrescriptionId

                // 3️⃣ Optional: reduce drug stock if linked
                if (model.SelectedDrugId.HasValue &&
                    model.DispensedQuantity.HasValue &&
                    model.DispensedQuantity.Value > 0)
                {
                    var drug = _db.Drugs.FirstOrDefault(d => d.DrugId == model.SelectedDrugId.Value);
                    if (drug == null)
                        return BadRequest("Selected drug not found.");

                    if (model.DispensedQuantity.Value > drug.Quantity)
                        return BadRequest($"Not enough stock for {drug.DrugName}. Current stock: {drug.Quantity}");

                    drug.Quantity -= model.DispensedQuantity.Value;
                    _db.Drugs.Update(drug);
                    _db.SaveChanges();
                }

                // 4️⃣ Build nice Diagnosis/Treatment text
                var diagnosisText = string.IsNullOrWhiteSpace(model.Notes)
                    ? "Medication prescription"
                    : model.Notes;

                var treatmentText = $"{model.Medication} — {model.Dosage}; {model.Frequency}; {model.Duration}"
                    .Trim()
                    .Trim(' ', ';', '—');

                if (string.IsNullOrWhiteSpace(treatmentText))
                {
                    treatmentText = "Medication prescribed (see details).";
                }

                // 5️⃣ Create linked MedicalRecord row
                var record = new MedicalRecord
                {
                    PetId = model.PetId,
                    StaffId = model.StaffId,
                    PrescriptionId = model.PrescriptionId, // 🔗 strong link
                    VaccinationId = null,
                    Date = model.Date,
                    Description = $"Prescription for {model.Medication}",
                    Diagnosis = diagnosisText,
                    Treatment = treatmentText
                };

                _db.MedicalRecords.Add(record);
                _db.SaveChanges();

                // 6️⃣ All good
                transaction.Commit();
                return Ok(new { message = "Prescription created successfully." });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, "Error saving prescription: " + ex.Message);
            }
        }
    



        // =========================================
        //  PRESCRIPTIONS — VIEW
        // =========================================
        [HttpGet]
        public IActionResult ViewPrescription(int id)
        {
            var pres = _db.Prescriptions
                .Include(p => p.Pet).ThenInclude(o => o.Owner)
                .Include(p => p.Staff)
                .FirstOrDefault(p => p.PrescriptionId == id);

            if (pres == null)
                return NotFound();

            return PartialView("_Modal_ViewPrescription", pres);
        }

        // =========================================
        //  PRESCRIPTIONS — EDIT
        // =========================================
        [HttpGet]
        public IActionResult EditPrescription(int id)
        {
            var pres = _db.Prescriptions.FirstOrDefault(p => p.PrescriptionId == id);
            if (pres == null) return NotFound();

            ViewBag.Pets = _db.Pets.Include(p => p.Owner).ToList();
            ViewBag.Staff = _db.Accounts
                .Where(a => a.IsAdmin == 2 || a.IsAdmin == 1)
                .ToList();
            ViewBag.Drugs = _db.Drugs.OrderBy(d => d.DrugName).ToList();

            return PartialView("_Modal_EditPrescription", pres);
        }

        [HttpPost]
        public IActionResult EditPrescription(Prescription model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid prescription data.");

            try
            {
                // 1️⃣ Update prescription
                _db.Prescriptions.Update(model);
                _db.SaveChanges();

                // 2️⃣ Update linked MedicalRecord (if exists)
                var record = _db.MedicalRecords
                    .FirstOrDefault(r => r.PrescriptionId == model.PrescriptionId);

                if (record != null)
                {
                    record.PetId = model.PetId;
                    record.StaffId = model.StaffId;
                    record.Date = model.Date;

                    var diagnosisText = string.IsNullOrWhiteSpace(model.Notes)
                        ? "Medication prescription"
                        : model.Notes;

                    var treatmentText = $"{model.Medication} — {model.Dosage}; {model.Frequency}; {model.Duration}"
                        .Trim()
                        .Trim(' ', ';', '—');

                    if (string.IsNullOrWhiteSpace(treatmentText))
                    {
                        treatmentText = "Medication prescribed (see details).";
                    }

                    record.Description = $"Prescription for {model.Medication}";
                    record.Diagnosis = diagnosisText;
                    record.Treatment = treatmentText;

                    _db.MedicalRecords.Update(record);
                    _db.SaveChanges();
                }

                return Ok(new { message = "Prescription updated." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error updating prescription: " + ex.Message);
            }
        }

        // =========================================
        //  PRESCRIPTIONS — DELETE
        // =========================================
        [HttpGet]
        public IActionResult DeletePrescription(int id)
        {
            var pres = _db.Prescriptions
                .Include(p => p.Pet).ThenInclude(o => o.Owner)
                .Include(p => p.Staff)
                .FirstOrDefault(p => p.PrescriptionId == id);

            if (pres == null)
                return NotFound();

            return PartialView("_Modal_DeletePrescription", pres);
        }

        [HttpPost]
        public IActionResult DeletePrescriptionConfirmed(int id)
        {
            var pres = _db.Prescriptions.Find(id);
            if (pres != null)
            {
                _db.Prescriptions.Remove(pres);
                _db.SaveChanges();
            }

            return Ok(new { message = "Prescription deleted." });
        }

        // =========================================
        //  DRUGS
        // =========================================
        public IActionResult DrugList()
        {
            var list = _db.Drugs
                .OrderBy(d => d.DrugName)
                .ToList();

            return PartialView("_Drugs", list);
        }

        [HttpGet]
        public IActionResult CreateDrug()
        {
            // DateAdded will be set here so it never ends up null
            var model = new Drug
            {
                DateAdded = DateTime.Now
            };

            return PartialView("_Modal_CreateDrug", model);
        }

        [HttpPost]
        public IActionResult CreateDrug(Drug model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid drug data.");

            // make sure date_added is never null (fixes the DB error)
            if (!model.DateAdded.HasValue)
                model.DateAdded = DateTime.Now;

            _db.Drugs.Add(model);
            _db.SaveChanges();

            return Ok(new { message = "Drug created successfully." });
        }

        [HttpGet]
        public IActionResult EditDrug(int id)
        {
            var drug = _db.Drugs.Find(id);
            if (drug == null)
                return NotFound();

            return PartialView("_Modal_EditDrug", drug);
        }

        [HttpPost]
        public IActionResult EditDrug(Drug model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid drug data.");

            var drug = _db.Drugs.FirstOrDefault(d => d.DrugId == model.DrugId);
            if (drug == null)
                return NotFound();

            // update editable fields only, keep original DateAdded
            drug.DrugName = model.DrugName;
            drug.DosageType = model.DosageType;
            drug.Quantity = model.Quantity;
            drug.UnitPrice = model.UnitPrice;
            drug.BasePrice = model.BasePrice;
            drug.ExpiryDate = model.ExpiryDate;
            drug.RestockNotes = model.RestockNotes;

            _db.SaveChanges();

            return Ok(new { message = "Drug updated successfully." });
        }

        [HttpGet]
        public IActionResult DeleteDrug(int id)
        {
            var drug = _db.Drugs.Find(id);
            if (drug == null)
                return NotFound();

            return PartialView("_Modal_DeleteDrug", drug);
        }

        [HttpPost]
        public IActionResult DeleteDrugConfirmed(int id)
        {
            var drug = _db.Drugs.Find(id);
            if (drug != null)
            {
                _db.Drugs.Remove(drug);
                _db.SaveChanges();
            }

            return Ok(new { message = "Drug deleted successfully." });
        }



        // =========================================
        //  VACCINATIONS
        // =========================================
        public IActionResult VaccinationList()
        {
            var list = _db.Vaccinations
                .Include(v => v.Pet).ThenInclude(o => o.Owner)
                .Include(v => v.Staff)
                .OrderByDescending(v => v.DateGiven)
                .ToList();

            return PartialView("_Vaccinations", list);
        }

        [HttpGet]
        public IActionResult CreateVaccination()
        {
            ViewBag.Pets = _db.Pets
                .Include(p => p.Owner)
                .ToList();

            ViewBag.Staff = _db.Accounts
                .Where(a => a.IsAdmin == 2 || a.IsAdmin == 1)
                .ToList();

            var model = new Vaccination
            {
                DateGiven = DateTime.Today,
                NextDueDate = DateTime.Today.AddYears(1)
            };

            return PartialView("_Modal_CreateVaccination", model);
        }

        // ====== UPDATED: CREATE VACCINATION (NO ModelState BLOCK + MESSAGE) ======
        [HttpPost]
        public IActionResult CreateVaccination(Vaccination model)
        {
            using var transaction = _db.Database.BeginTransaction();

            try
            {
                // 1️⃣ Default dates if empty
                if (!model.DateGiven.HasValue)
                    model.DateGiven = DateTime.Today;

                if (!model.NextDueDate.HasValue)
                    model.NextDueDate = model.DateGiven.Value.AddYears(1);

                // 2️⃣ Save vaccination
                _db.Vaccinations.Add(model);
                _db.SaveChanges(); // fills model.VaccinationId

                // 3️⃣ Build default Diagnosis/Treatment
                var diagnosisText = "Vaccination / preventive care";

                var nextDueText = model.NextDueDate.HasValue
                    ? $"Next due: {model.NextDueDate.Value:MMM dd, yyyy}"
                    : "Next dose as advised";

                var treatmentText = $"{model.VaccineName} administered. {nextDueText}";

                // 4️⃣ Create linked MedicalRecord row
                var record = new MedicalRecord
                {
                    PetId = model.PetId,
                    StaffId = model.StaffId,
                    PrescriptionId = null,
                    VaccinationId = model.VaccinationId, // 🔗 strong link
                    Date = model.DateGiven,
                    Description = $"Vaccination: {model.VaccineName}",
                    Diagnosis = diagnosisText,
                    Treatment = treatmentText
                };

                _db.MedicalRecords.Add(record);
                _db.SaveChanges();

                transaction.Commit();

                return Ok(new
                {
                    success = true,
                    message = "Vaccination record added successfully."
                });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, "Error saving vaccination: " + ex.Message);
            }
        }



        [HttpGet]
        public IActionResult ViewVaccination(int id)
        {
            var vacc = _db.Vaccinations
                .Include(v => v.Pet).ThenInclude(o => o.Owner)
                .Include(v => v.Staff)
                .FirstOrDefault(v => v.VaccinationId == id);

            if (vacc == null)
                return NotFound();

            return PartialView("_Modal_ViewVaccination", vacc);
        }

        [HttpGet]
        public IActionResult EditVaccination(int id)
        {
            var vacc = _db.Vaccinations
                .FirstOrDefault(v => v.VaccinationId == id);

            if (vacc == null)
                return NotFound();

            ViewBag.Pets = _db.Pets
                .Include(p => p.Owner)
                .ToList();

            ViewBag.Staff = _db.Accounts
                .Where(a => a.IsAdmin == 2 || a.IsAdmin == 1)
                .ToList();

            return PartialView("_Modal_EditVaccination", vacc);
        }

        // ====== UPDATED: EDIT VACCINATION (NO ModelState BLOCK + MESSAGE) ======
        [HttpPost]
        public IActionResult EditVaccination(Vaccination model)
        {
            try
            {
                // 1️⃣ Update vaccination itself
                _db.Vaccinations.Update(model);
                _db.SaveChanges();

                // 2️⃣ Update linked MedicalRecord if it exists
                var record = _db.MedicalRecords
                    .FirstOrDefault(r => r.VaccinationId == model.VaccinationId);

                if (record != null)
                {
                    record.PetId = model.PetId;
                    record.StaffId = model.StaffId;
                    record.Date = model.DateGiven ?? record.Date;

                    var diagnosisText = "Vaccination / preventive care";

                    var nextDueText = model.NextDueDate.HasValue
                        ? $"Next due: {model.NextDueDate.Value:MMM dd, yyyy}"
                        : "Next dose as advised";

                    var treatmentText = $"{model.VaccineName} administered. {nextDueText}";

                    record.Description = $"Vaccination: {model.VaccineName}";
                    record.Diagnosis = diagnosisText;
                    record.Treatment = treatmentText;

                    _db.MedicalRecords.Update(record);
                    _db.SaveChanges();
                }

                return Ok(new
                {
                    success = true,
                    message = "Vaccination record updated successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error updating vaccination: " + ex.Message);
            }
        }


        [HttpGet]
        public IActionResult DeleteVaccination(int id)
        {
            var vacc = _db.Vaccinations
                .Include(v => v.Pet).ThenInclude(o => o.Owner)
                .Include(v => v.Staff)
                .FirstOrDefault(v => v.VaccinationId == id);

            if (vacc == null)
                return NotFound();

            return PartialView("_Modal_DeleteVaccination", vacc);
        }

        [HttpPost]
        public IActionResult DeleteVaccinationConfirmed(int id)
        {
            var vacc = _db.Vaccinations.Find(id);
            if (vacc != null)
            {
                _db.Vaccinations.Remove(vacc);
                _db.SaveChanges();
            }

            return Ok(new { message = "Vaccination deleted." });
        }
    }
}
