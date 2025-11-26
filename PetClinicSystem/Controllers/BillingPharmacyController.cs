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
            if (!ModelState.IsValid)
                return BadRequest("Invalid prescription data.");

            using var transaction = _db.Database.BeginTransaction();
            try
            {
                _db.Prescriptions.Add(model);

                // Optional drug link: SelectedDrugId + DispensedQuantity (if you added those NotMapped props)
                if (model.SelectedDrugId.HasValue &&
                    model.DispensedQuantity.HasValue &&
                    model.DispensedQuantity.Value > 0)
                {
                    var drug = _db.Drugs
                        .FirstOrDefault(d => d.DrugId == model.SelectedDrugId.Value);

                    if (drug != null)
                    {
                        if (drug.Quantity < model.DispensedQuantity.Value)
                        {
                            return BadRequest("Not enough stock for selected drug.");
                        }

                        drug.Quantity -= model.DispensedQuantity.Value;
                        _db.Drugs.Update(drug);
                    }
                }

                _db.SaveChanges();
                transaction.Commit();

                return Ok(new { message = "Prescription created." });
            }
            catch
            {
                transaction.Rollback();
                return StatusCode(500, "Error saving prescription.");
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
            var pres = _db.Prescriptions
                .FirstOrDefault(p => p.PrescriptionId == id);

            if (pres == null)
                return NotFound();

            ViewBag.Pets = _db.Pets
                .Include(p => p.Owner)
                .ToList();

            ViewBag.Staff = _db.Accounts
                .Where(a => a.IsAdmin == 2 || a.IsAdmin == 1)
                .ToList();

            return PartialView("_Modal_EditPrescription", pres);
        }

        [HttpPost]
        public IActionResult EditPrescription(Prescription model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid prescription data.");

            _db.Prescriptions.Update(model);
            _db.SaveChanges();

            return Ok(new { message = "Prescription updated." });
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
            return PartialView("_Modal_CreateDrug", new Drug());
        }

        [HttpPost]
        public IActionResult CreateDrug(Drug model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid drug data.");

            _db.Drugs.Add(model);
            _db.SaveChanges();

            return Ok(new { message = "Drug created." });
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

            _db.Drugs.Update(model);
            _db.SaveChanges();

            return Ok(new { message = "Drug updated." });
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

            return Ok(new { message = "Drug deleted." });
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
            try
            {
                // Default dates if empty
                if (!model.DateGiven.HasValue)
                    model.DateGiven = DateTime.Today;

                if (!model.NextDueDate.HasValue)
                    model.NextDueDate = model.DateGiven.Value.AddYears(1);

                _db.Vaccinations.Add(model);
                _db.SaveChanges();

                return Ok(new
                {
                    success = true,
                    message = "Vaccination record added successfully."
                });
            }
            catch (Exception ex)
            {
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
                _db.Vaccinations.Update(model);
                _db.SaveChanges();

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
