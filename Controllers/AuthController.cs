using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
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

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    
    [HttpPost]
    public IActionResult Register(string fullName, string email, string password)
    {
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
            Role = role
        };

        user.PasswordHash = _hasher.HashPassword(user, password);

        _context.Users.Add(user);
        _context.SaveChanges();

        if (role == "Parent")
        {
            var username = email.Split('@')[0];
            var child = _context.Users.FirstOrDefault(u =>
                u.Role == "Student" &&
                u.Email.StartsWith(username));

            if (child != null)
            {
                child.ParentId = user.Id;
                _context.SaveChanges();
            }
        }

        TempData["Success"] = "Account created successfully. You can now log in.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string email, string password)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == email);

        if (user == null)
        {
            ViewBag.Error = "Invalid email or password.";
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
        HttpContext.Session.SetString("Email", user.Email);

        //  ВИНАГИ КЪМ HOME
        return RedirectToAction("Index", "Home");
    }


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
