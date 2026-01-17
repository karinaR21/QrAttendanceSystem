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
    public IActionResult Register(string email, string password)
    {
        if (_context.Users.Any(u => u.Email == email))
            return Content("User already exists");

        string role;

        if (email.EndsWith("@teacher.school.bg"))
            role = "Teacher";
        else if (email.EndsWith("@student.school.bg"))
            role = "Student";
        else if (email.EndsWith("@parent.school.bg"))
            role = "Parent";
        else
            return Content("Invalid email domain");

        var user = new User
        {
            Email = email,
            Role = role
        };


        user.PasswordHash = _hasher.HashPassword(user, password);

        _context.Users.Add(user);
        _context.SaveChanges();

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
            return Content("Invalid credentials");

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);

        if (result == PasswordVerificationResult.Failed)
            return Content("Invalid credentials");

        if (string.IsNullOrEmpty(user.Role))
            return Content("User has no role assigned");

        //  СЕСИЯ
        HttpContext.Session.SetString("Role", user.Role);
        HttpContext.Session.SetInt32("UserId", user.Id);

        if (user.Role == "Student")
        {
            HttpContext.Session.SetInt32("StudentId", user.Id);
        }

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
