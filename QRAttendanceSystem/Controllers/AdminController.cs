using Microsoft.AspNetCore.Mvc;
using QRAttendanceSystem.Data;

namespace QRAttendanceSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("Role") == "Admin";
        }

        public IActionResult Index()
        {
            if (!IsAdmin())
                return Unauthorized();

            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalTeachers = _context.Users.Count(u => u.Role == "Teacher");
            ViewBag.TotalStudents = _context.Users.Count(u => u.Role == "Student");
            ViewBag.TotalParents = _context.Users.Count(u => u.Role == "Parent");
            ViewBag.TotalAdmins = _context.Users.Count(u => u.Role == "Admin");

            ViewBag.TotalActiveUsers = _context.Users.Count(u => u.IsActive);
            ViewBag.TotalInactiveUsers = _context.Users.Count(u => !u.IsActive);

            ViewBag.TotalSessions = _context.Sessions.Count();
            ViewBag.TotalAttendances = _context.Attendances.Count();

            ViewBag.ByStatus = _context.Attendances
                .GroupBy(a => a.Status)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToList();

            return View();
        }

        public IActionResult Users()
        {
            if (!IsAdmin())
                return Unauthorized();

            var users = _context.Users
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FullName)
                .ToList();

            return View(users);
        }

        [HttpPost]
        public IActionResult DeactivateUser(int id)
        {
            if (!IsAdmin())
                return Unauthorized();

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == id)
                return BadRequest("You cannot deactivate your own admin account.");

            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound();

            user.IsActive = false;
            _context.SaveChanges();

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public IActionResult ActivateUser(int id)
        {
            if (!IsAdmin())
                return Unauthorized();

            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound();

            user.IsActive = true;
            _context.SaveChanges();

            return RedirectToAction(nameof(Users));
        }
    }
}