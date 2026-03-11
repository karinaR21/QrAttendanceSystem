using Microsoft.AspNetCore.Mvc;
using QRAttendanceSystem.Data;

namespace QRAttendanceSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult _StudentHome()
        {
            if (HttpContext.Session.GetString("Role") != "Student")
                return RedirectToAction("Index");

            return View();
        }

        public IActionResult _TeacherHome()
        {
            if (HttpContext.Session.GetString("Role") != "Teacher")
                return RedirectToAction("Index");

            return View();
        }

        public IActionResult _ParentHome()
        {
            if (HttpContext.Session.GetString("Role") != "Parent")
                return RedirectToAction("Index");

            var parentId = HttpContext.Session.GetInt32("UserId");

            var child = _context.Users
                .FirstOrDefault(u => u.ParentId == parentId && u.Role == "Student");

            if (child != null)
            {
                var attendances = _context.Attendances
                    .Where(a => a.UserId == child.Id)
                    .ToList();

                int total = attendances.Count;
                int present = attendances.Count(a => a.Status.ToString() == "Present");
                int absent = attendances.Count(a => a.Status.ToString() == "Absent");
                int late = attendances.Count(a => a.Status.ToString() == "Late");

                double percentage = total == 0
                    ? 0
                    : (double)present / total * 100;

                string riskLevel;

                if (percentage < 75 || absent >= 5)
                    riskLevel = "High";
                else if (percentage < 85 || late >= 3)
                    riskLevel = "Warning";
                else
                    riskLevel = "Good";

                ViewBag.RiskLevel = riskLevel;
                ViewBag.AttendancePercentage = percentage.ToString("0");
            }

            return View();
        }
    }
}