using Microsoft.AspNetCore.Mvc;
using PetClinicSystem.Data;
using PetClinicSystem.Models;
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

        // Hash Password
        private string HashPassword(string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }
        //login
        [HttpPost]
        public IActionResult Login(string Username, string Password)
        {
            var hash = HashPassword(Password);

            var user = _db.Accounts
                .FirstOrDefault(a => a.Username == Username &&
                                     a.PasswordHash == hash &&
                                     a.IsActive == true);

            if (user == null)
            {
                TempData["Error"] = "Invalid username or password.";
                return RedirectToAction("Index", "Home");
            }

            // Set session
            HttpContext.Session.SetInt32("UserId", user.AccountId);
            HttpContext.Session.SetInt32("UserRole", user.IsAdmin);
            HttpContext.Session.SetString("UserName", user.FullName);

            // Show success message
            TempData["Success"] = $"Welcome back, {user.FullName}!";

            // Redirect by role
            return user.IsAdmin switch
            {
                1 => RedirectToAction("Index", "Admin"),
                2 => RedirectToAction("Index", "Staff"),
                _ => RedirectToAction("Index", "Client")
            };
        }


        // SIGN UP POST
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

    }
}
