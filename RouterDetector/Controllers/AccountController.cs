using Microsoft.AspNetCore.Mvc;
using RouterDetector.Models;
using RouterDetector.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace RouterDetector.Controllers
{
    public class AccountController : Controller
    {
        private readonly RouterDetectorContext _context;
        public AccountController(RouterDetectorContext context)
        {
            _context = context;
        }

        // Registration functionality commented out as requested
        /*
        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.Saccos = _context.SystemConfiguration.ToList();
            ViewBag.Counties = _context.SystemConfiguration.Select(s => s.County).Distinct().OrderBy(c => c).ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string email, string username, string password, string staffPosition, int? saccoId, string newSacco, string newCounty, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(staffPosition))
            {
                ViewBag.Error = "All fields are required.";
                ViewBag.Saccos = _context.SystemConfiguration.ToList();
                ViewBag.Counties = _context.SystemConfiguration.Select(s => s.County).Distinct().OrderBy(c => c).ToList();
                return View();
            }
            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                ViewBag.Saccos = _context.SystemConfiguration.ToList();
                ViewBag.Counties = _context.SystemConfiguration.Select(s => s.County).Distinct().OrderBy(c => c).ToList();
                return View();
            }
            // Only one of saccoId or newSacco should be provided
            bool hasDropdown = saccoId.HasValue && saccoId.Value != 0;
            bool hasNewSacco = !string.IsNullOrWhiteSpace(newSacco);
            if ((hasDropdown && hasNewSacco) || (!hasDropdown && !hasNewSacco))
            {
                ModelState.AddModelError("", "Please select a Sacco or enter a new Sacco name, but not both.");
                ViewBag.Saccos = _context.SystemConfiguration.ToList();
                ViewBag.Counties = _context.SystemConfiguration.Select(s => s.County).Distinct().OrderBy(c => c).ToList();
                return View();
            }
            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            {
                ModelState.AddModelError("", "Invalid email format.");
                ViewBag.Saccos = _context.SystemConfiguration.ToList();
                ViewBag.Counties = _context.SystemConfiguration.Select(s => s.County).Distinct().OrderBy(c => c).ToList();
                return View();
            }
            if (_context.Users.Any(u => u.Username == username))
            {
                ModelState.AddModelError("", "Username already exists.");
                ViewBag.Saccos = _context.SystemConfiguration.ToList();
                ViewBag.Counties = _context.SystemConfiguration.Select(s => s.County).Distinct().OrderBy(c => c).ToList();
                return View();
            }
            if (_context.Users.Any(u => u.Email == email))
            {
                ModelState.AddModelError("", "Email already registered.");
                ViewBag.Saccos = _context.SystemConfiguration.ToList();
                ViewBag.Counties = _context.SystemConfiguration.Select(s => s.County).Distinct().OrderBy(c => c).ToList();
                return View();
            }
            int finalSaccoId = 0;
            if (hasNewSacco)
            {
                if (string.IsNullOrWhiteSpace(newCounty))
                {
                    ModelState.AddModelError("", "County is required when adding a new Sacco.");
                    ViewBag.Saccos = _context.SystemConfiguration.ToList();
                    ViewBag.Counties = _context.SystemConfiguration.Select(s => s.County).Distinct().OrderBy(c => c).ToList();
                    return View();
                }
                // Check if Sacco exists (case-insensitive)
                var existingSacco = _context.SystemConfiguration.FirstOrDefault(s => s.InstitutionName.ToLower() == newSacco.Trim().ToLower());
                if (existingSacco != null)
                {
                    finalSaccoId = existingSacco.Id;
                }
                else
                {
                    var newSaccoObj = new SystemConfiguration
                    {
                        InstitutionName = newSacco.Trim(),
                        StaffPosition = staffPosition.Trim(),
                        County = newCounty.Trim()
                    };
                    _context.SystemConfiguration.Add(newSaccoObj);
                    await _context.SaveChangesAsync();
                    finalSaccoId = newSaccoObj.Id;
                }
            }
            else if (hasDropdown)
            {
                finalSaccoId = saccoId.Value;
            }
            else
            {
                ModelState.AddModelError("", "Please select a Sacco or enter a new Sacco name.");
                ViewBag.Saccos = _context.SystemConfiguration.ToList();
                ViewBag.Counties = _context.SystemConfiguration.Select(s => s.County).Distinct().OrderBy(c => c).ToList();
                return View();
            }
            var user = new User
            {
                Email = email,
                Username = username,
                PasswordHash = HashPassword(password),
                SaccoId = finalSaccoId,
                StaffPosition = staffPosition.Trim()
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("Login");
        }
        */

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            User user = null;

            // First, try to find user by username or email (case-insensitive)
            user = await _context.Users.FirstOrDefaultAsync(u =>
                (u.Username.ToLower() == username.ToLower() || u.Email.ToLower() == username.ToLower()) &&
                u.IsActive);

            if (user == null)
            {
                ViewBag.Error = "Invalid credentials.";
                return View();
            }

            // Check password
            if (user.PasswordHash != HashPassword(password))
            {
                ViewBag.Error = "Invalid credentials.";
                return View();
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("StaffPosition", user.StaffPosition),
                new Claim("SaccoId", user.SaccoId.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return RedirectToAction("Dashboard", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var username = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            
            if (user == null)
            {
                ViewBag.Error = "User not found.";
                return View();
            }

            // Check current password
            if (user.PasswordHash != HashPassword(currentPassword))
            {
                ViewBag.Error = "Current password is incorrect.";
                return View();
            }

            // Validate new password
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ViewBag.Error = "New password cannot be empty.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "New password and confirmation do not match.";
                return View();
            }

            // Update password
            user.PasswordHash = HashPassword(newPassword);
            await _context.SaveChangesAsync();
            
            ViewBag.Success = "Password changed successfully!";
            return View();
        }

        // Forgot password functionality commented out as requested
        /*
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string identifier)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == identifier || u.Email == identifier);
            if (user == null)
            {
                ViewBag.Error = "No user found with that username or email.";
                return View();
            }
            // Show the reset form
            ViewBag.Identifier = identifier;
            ViewBag.ShowResetForm = true;
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string identifier, string newPassword)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == identifier || u.Email == identifier);
            if (user == null)
            {
                ViewBag.Error = "No user found with that username or email.";
                return View("ForgotPassword");
            }
            user.PasswordHash = HashPassword(newPassword);
            _context.SaveChanges();
            ViewBag.Success = "Password has been reset. You can now log in.";
            return View("ForgotPassword");
        }
        */

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
} 