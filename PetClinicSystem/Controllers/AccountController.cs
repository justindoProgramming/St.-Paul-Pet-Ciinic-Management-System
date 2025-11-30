using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PetClinicSystem.Data;
using PetClinicSystem.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PetClinicSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AccountController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ===================== HASH HELPER =====================
        private string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return null;

            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hashBytes = sha.ComputeHash(bytes);
                var sb = new StringBuilder();
                foreach (var b in hashBytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // ===================== LANDING / DEFAULT =====================

        // /Account or /Account/Index  → send to public landing page (Home/Index)
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Home");
        }

        // ===================== LOGIN =====================

        [HttpGet]
        public IActionResult Login()
        {
            // We don't have a /Views/Account/Login view.
            // If someone browses here directly, send them back to Home
            // and open the login modal.
            TempData["OpenLoginModal"] = "true";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Username and password are required.";
                TempData["OpenLoginModal"] = "true";
                return RedirectToAction("Index", "Home");
            }

            var hash = HashPassword(password);

            var user = _db.Accounts
                .FirstOrDefault(a =>
                    a.Username == username &&
                    a.PasswordHash == hash &&
                    a.IsActive == true);

            if (user == null)
            {
                TempData["Error"] = "Invalid username or password, or account is inactive.";
                TempData["OpenLoginModal"] = "true";
                return RedirectToAction("Index", "Home");
            }

            // save info in session for layouts
            HttpContext.Session.SetInt32("UserId", user.AccountId);
            HttpContext.Session.SetString("UserName", user.FullName ?? user.Username);
            HttpContext.Session.SetInt32("UserRole", user.IsAdmin);  // 1=Admin, 2=Staff, 0=Client

            // redirect based on role
            if (user.IsAdmin == 1)
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (user.IsAdmin == 2)
            {
                return RedirectToAction("Index", "Staff");
            }
            else
            {
                // client
                return RedirectToAction("Index", "Client");
            }
        }

        // ===================== REGISTER =====================

        [HttpPost]
        public IActionResult Register(string FullName, string Email, string Username, string Password)
        {
            if (_db.Accounts.Any(a => a.Email == Email || a.Username == Username))
            {
                TempData["Error"] = "Email or username already exists.";
                return RedirectToAction("Index", "Home");
            }

            var account = new Account()
            {
                FullName = FullName,
                Email = Email,
                Username = Username,
                PasswordHash = HashPassword(Password),
                IsAdmin = 0,              // client by default
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _db.Accounts.Add(account);
            _db.SaveChanges();

            TempData["Success"] = "Account created successfully! You may now log in.";

            return RedirectToAction("Index", "Home");
        }

        // ===================== LOGOUT =====================
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["LogoutMessage"] = "You have been logged out.";
            return RedirectToAction("Index", "Home");
        }

        // ===================== PROFILE (ALL ROLES) =====================

        [HttpGet]
        public IActionResult Profile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var acc = _db.Accounts.FirstOrDefault(a => a.AccountId == userId.Value);
            if (acc == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Index", "Home");
            }

            var model = new AccountProfileViewModel
            {
                FullName = acc.FullName,
                Email = acc.Email,
                Username = acc.Username
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(AccountProfileViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var acc = _db.Accounts.FirstOrDefault(a => a.AccountId == userId.Value);
            if (acc == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Index", "Home");
            }

            // unique email / username (excluding self)
            bool exists = _db.Accounts.Any(a =>
                a.AccountId != acc.AccountId &&
                (a.Email == model.Email || a.Username == model.Username));

            if (exists)
            {
                ModelState.AddModelError(string.Empty, "Email or username is already in use.");
                return View(model);
            }

            acc.FullName = model.FullName;
            acc.Email = model.Email;
            acc.Username = model.Username;
            acc.UpdatedAt = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                acc.PasswordHash = HashPassword(model.NewPassword);
            }

            _db.SaveChanges();

            // refresh name in header
            HttpContext.Session.SetString("UserName", acc.FullName ?? acc.Username);

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction("Profile");
        }
    }
}
