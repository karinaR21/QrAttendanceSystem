using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QRAttendanceSystem.Data;
using QRAttendanceSystem.Models;

public class AuthController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ================= REGISTER =================

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(string fullName, string email, string password, string? childEmail)
    {
        if (string.IsNullOrEmpty(password) || !System.Text.RegularExpressions.Regex.IsMatch(password, "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)[A-Za-z\\d]{8,12}$"))
        {
            ViewBag.Error = "Password must be 8-12 characters and include uppercase, lowercase letters and numbers.";
            return View();
        }

        if (_context.Users.Any(u => u.Email == email))
        {
            ViewBag.Error = "User with this email already exists.";
            return View();
        }

        string role;

        if (email.EndsWith("@teacher.school.bg"))
            role = "Teacher";
        else if (email.EndsWith("@student.school.bg"))
            role = "Student";
        else if (email.EndsWith("@parent.school.bg"))
            role = "Parent";
        else
        {
            ViewBag.Error = "Invalid email domain.";
            return View();
        }

        var user = new User
        {
            FullName = fullName,
            Email = email,
            Role = role,
            IsActive = true
        };

        user.PasswordHash = _hasher.HashPassword(user, password);

        _context.Users.Add(user);
        _context.SaveChanges();

        if (role == "Parent")
        {
            User? child = null;

            if (!string.IsNullOrEmpty(childEmail))
            {
                child = _context.Users.FirstOrDefault(u =>
                    u.Role == "Student" &&
                    u.Email == childEmail);
            }

            if (child == null)
            {
                var local = email.Split('@')[0];

                string guessedChildEmail;
                if (local.EndsWith(".parent", StringComparison.OrdinalIgnoreCase))
                {
                    var prefix = local.Substring(0, local.Length - ".parent".Length);
                    guessedChildEmail = prefix + "@student.school.bg";
                }
                else
                {
                    guessedChildEmail = local + "@student.school.bg";
                }

                child = _context.Users.FirstOrDefault(u =>
                    u.Role == "Student" &&
                    (u.Email!.Equals(guessedChildEmail, StringComparison.OrdinalIgnoreCase) ||
                     u.Email.StartsWith(local, StringComparison.OrdinalIgnoreCase)));
            }

            if (child != null)
            {
                child.ParentId = user.Id;
                _context.SaveChanges();
            }
        }

        TempData["Success"] = "Account created successfully. You can now log in.";
        return RedirectToAction("Login");
    }

    // ================= LOGIN =================

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string email, string password, bool rememberMe)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == email);

        if (user == null)
        {
            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        if (!user.IsActive)
        {
            ViewBag.Error = "Your account has been deactivated. Please contact the administrator.";
            return View();
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);

        if (result == PasswordVerificationResult.Failed)
        {
            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        HttpContext.Session.SetString("Role", user.Role);
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("Email", user.Email ?? "");

        if (rememberMe)
        {
            Response.Cookies.Append(
                "RememberEmail",
                email,
                new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(30),
                    Path = "/"
                });
        }
        else
        {
            Response.Cookies.Delete("RememberEmail");
        }

        if (user.Role == "Admin")
            return RedirectToAction("Index", "Admin");

        if (user.Role == "Teacher")
            return RedirectToAction("_TeacherHome", "Home");

        if (user.Role == "Student")
            return RedirectToAction("_StudentHome", "Home");

        if (user.Role == "Parent")
            return RedirectToAction("_ParentHome", "Home");

        return RedirectToAction("Index", "Home");
    }

    // ================= SEED ADMIN =================

    [HttpGet]
    public IActionResult SeedAdmin()
    {
        if (_context.Users.Any(u => u.Role == "Admin"))
            return Content("Admin already exists.");

        var admin = new User
        {
            FullName = "System Administrator",
            Email = "admin@school.bg",
            Role = "Admin",
            IsActive = true
        };

        admin.PasswordHash = _hasher.HashPassword(admin, "Admin123");

        _context.Users.Add(admin);
        _context.SaveChanges();

        return Content("Admin created successfully.");
    }

    // ================= LOGOUT =================

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
    
    public IActionResult Index()
    {
        return View();
    }
}