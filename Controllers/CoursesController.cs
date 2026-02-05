using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QRAttendanceSystem.Data;
using QRAttendanceSystem.Models;

namespace QRAttendanceSystem.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var courses = _context.Courses.ToList();
            return View(courses);
        }


        // CREATE - GET
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Teacher")
                return Unauthorized();

            // 🔽 DROPDOWN от базата
            ViewBag.Teachers = new SelectList(
                _context.Users.Where(u => u.Role == "Teacher"),
                "Id",
                "Email" // или FullName
            );

            return View();
        }

        // CREATE - POST
        [HttpPost]
        public IActionResult Create(Course course)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Teacher")
                return Unauthorized();

            if (!ModelState.IsValid)
            {
                // ако има грешка – презареждаме dropdown-а
                ViewBag.Teachers = new SelectList(
                    _context.Users.Where(u => u.Role == "Teacher"),
                    "Id",
                    "Email"
                );
                return View(course);
            }

            _context.Courses.Add(course);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }
    }
}
