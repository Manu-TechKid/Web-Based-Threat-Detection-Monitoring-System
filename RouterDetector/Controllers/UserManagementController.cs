using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RouterDetector.Data;
using RouterDetector.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace RouterDetector.Controllers
{
    [Authorize]
    public class UserManagementController : Controller
    {
        private readonly RouterDetectorContext _context;
        public UserManagementController(RouterDetectorContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var staffPosition = User?.Claims?.FirstOrDefault(c => c.Type == "StaffPosition")?.Value;
            return staffPosition != null && staffPosition.ToLower() == "admin";
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!IsAdmin())
            {
                context.Result = Forbid();
            }
            base.OnActionExecuting(context);
        }

        // GET: UserManagement
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.Include(u => u.Sacco).AsNoTracking().ToListAsync();
            return View(users);
        }

        // GET: UserManagement/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            ViewBag.Saccos = await _context.SystemConfiguration.OrderBy(s => s.InstitutionName).ToListAsync();
            return View(user);
        }

        // POST: UserManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string email, string staffPosition, int saccoId, bool isActive)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            
            // Validate staff position
            if (staffPosition != "Admin" && staffPosition != "User")
            {
                TempData["Error"] = "Staff position must be either 'Admin' or 'User'.";
                return RedirectToAction(nameof(Index));
            }
            
            user.Email = email.Trim().ToLower(); // Normalize email
            user.StaffPosition = staffPosition;
            user.SaccoId = saccoId;
            user.IsActive = isActive;
            await _context.SaveChangesAsync();
            await LogAudit($"Edited user {user.Username} - Position: {user.StaffPosition}, Active: {user.IsActive}");
            return RedirectToAction(nameof(Index));
        }

        // POST: UserManagement/ResetPassword/5
        [HttpPost]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.PasswordHash = HashPassword(newPassword);
            await _context.SaveChangesAsync();
            await LogAudit($"Reset password for user {user.Username}");
            return RedirectToAction(nameof(Index));
        }

        // POST: UserManagement/ToggleActive/5
        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();
            await LogAudit($"Toggled active status for user {user.Username} to {user.IsActive}");
            return RedirectToAction(nameof(Index));
        }

        // GET: UserManagement/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Saccos = await _context.SystemConfiguration.OrderBy(s => s.InstitutionName).ToListAsync();
            ViewBag.StaffPositions = new List<string> { "Admin", "User" };
            return View();
        }

        // POST: UserManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string username, string email, string password, string staffPosition, int saccoId)
        {
            // Validate all required fields
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(staffPosition) || saccoId == 0)
            {
                ViewBag.Error = "All fields are required.";
                ViewBag.Saccos = _context.SystemConfiguration.OrderBy(s => s.InstitutionName).ToList();
                ViewBag.StaffPositions = new List<string> { "Admin", "User" };
                return View();
            }

            // Check if username already exists (case-insensitive)
            if (_context.Users.Any(u => u.Username.ToLower() == username.ToLower()))
            {
                ViewBag.Error = "Username already exists.";
                ViewBag.Saccos = _context.SystemConfiguration.OrderBy(s => s.InstitutionName).ToList();
                ViewBag.StaffPositions = new List<string> { "Admin", "User" };
                return View();
            }

            // Check if email already exists (case-insensitive)
            if (_context.Users.Any(u => u.Email.ToLower() == email.ToLower()))
            {
                ViewBag.Error = "Email already registered.";
                ViewBag.Saccos = _context.SystemConfiguration.OrderBy(s => s.InstitutionName).ToList();
                ViewBag.StaffPositions = new List<string> { "Admin", "User" };
                return View();
            }

            // Validate staff position
            if (staffPosition != "Admin" && staffPosition != "User")
            {
                ViewBag.Error = "Staff position must be either 'Admin' or 'User'.";
                ViewBag.Saccos = _context.SystemConfiguration.OrderBy(s => s.InstitutionName).ToList();
                ViewBag.StaffPositions = new List<string> { "Admin", "User" };
                return View();
            }

            var user = new User
            {
                Username = username.Trim(), // Trim whitespace
                Email = email.Trim().ToLower(), // Normalize email to lowercase
                PasswordHash = HashPassword(password),
                StaffPosition = staffPosition,
                SaccoId = saccoId,
                IsActive = true
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            await LogAudit($"Created user {user.Username} with position {user.StaffPosition}");
            return RedirectToAction(nameof(Index));
        }

        // GET: UserManagement/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.Include(u => u.Sacco).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            // Prevent admin from deleting themselves
            if (User.Identity.Name == user.Username)
            {
                TempData["Error"] = "You cannot delete your own account while logged in.";
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // POST: UserManagement/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            if (User.Identity.Name == user.Username)
            {
                TempData["Error"] = "You cannot delete your own account while logged in.";
                return RedirectToAction(nameof(Index));
            }
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            await LogAudit($"Deleted user {user.Username}");
            return RedirectToAction(nameof(Index));
        }

        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private async Task LogAudit(string action)
        {
            var audit = new AuditTrail
            {
                Username = User.Identity.Name,
                Action = action,
                Timestamp = DateTime.Now
            };
            _context.AuditTrails.Add(audit);
            await _context.SaveChangesAsync();
        }
    }
} 