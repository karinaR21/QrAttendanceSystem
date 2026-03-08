using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QRAttendanceSystem.Data;
using QRAttendanceSystem.Models;
using QRAttendanceSystem.ViewModels;

namespace QRAttendanceSystem.Controllers
{
    public class SessionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SessionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Teacher")
                return Unauthorized();

            var teacherId = HttpContext.Session.GetInt32("UserId");
            if (teacherId == null)
                return RedirectToAction("Login", "Auth");

            var sessions = _context.Sessions
                .Include(s => s.Course)
                .Where(s => s.TeacherId == teacherId.Value)
                .OrderByDescending(s => s.StartTime)
                .ToList();

            return View(sessions);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Teacher")
                return Unauthorized();

            var vm = new CreateSessionViewModel
            {
                Courses = _context.Courses
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    })
                    .ToList(),

                SessionDate = DateTime.Today
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult Create(CreateSessionViewModel vm)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Teacher")
                return Unauthorized();

            var teacherId = HttpContext.Session.GetInt32("UserId");
            if (teacherId == null)
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                vm.Courses = _context.Courses
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    })
                    .ToList();

                return View(vm);
            }

            var start = vm.SessionDate!.Value.Date.Add(vm.StartTime!.Value);

            var session = new Session
            {
                CourseId = vm.CourseId!.Value,
                TeacherId = teacherId.Value,
                StartTime = start,
                PresentUntil = start.AddMinutes(3),
                LateUntil = start.AddMinutes(10),
                EndTime = start.AddMinutes(45),
                Date = start.Date,
                Grade = vm.Grade!.Value,
                Section = vm.Section!
            };

            _context.Sessions.Add(session);
            _context.SaveChanges();

            return RedirectToAction("Generate", "Attendance", new { sessionId = session.Id });
        }
    }
}