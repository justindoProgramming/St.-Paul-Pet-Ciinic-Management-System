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
            // if you want login form on Home/Index only, you can
            // also redirect there instead of returning a view here.
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Username and password are required.";
                return View();
            }

            var hash = HashPassword(password);

            var user = _db.Accounts
                .FirstOrDefault(a =>
                    a.Username == username &&
                    a.PasswordHash == hash &&
                    a.IsActive == true);

            if (user == null)
            {
                ViewBag.Error = "Invalid username or password, or account is inactive.";
                return View();
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
                IsAdmin = 0,
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
            // Clear all session data
            HttpContext.Session.Clear();

            // One-time message for the next request
            TempData["LogoutMessage"] = "You have been logged out.";

            // Go back to the public landing page
            return RedirectToAction("Index", "Home");
        }


    }
}
