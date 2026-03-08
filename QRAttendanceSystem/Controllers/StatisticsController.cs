using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QRAttendanceSystem.Data;

namespace QRAttendanceSystem.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StatisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= ADMIN / TEACHER =================
        public IActionResult Index(int? grade, string? section, DateTime? from, DateTime? to)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Teacher")
                return Unauthorized();

            var teacherId = HttpContext.Session.GetInt32("UserId");
            if (teacherId == null)
                return RedirectToAction("Login", "Auth");

            var sessionsQuery = _context.Sessions
                .Where(s => s.TeacherId == teacherId.Value)
                .AsQueryable();

            if (grade.HasValue)
                sessionsQuery = sessionsQuery.Where(s => s.Grade == grade.Value);

            if (!string.IsNullOrEmpty(section))
                sessionsQuery = sessionsQuery.Where(s => s.Section == section);

            if (from.HasValue)
                sessionsQuery = sessionsQuery.Where(s => s.Date >= from.Value);

            if (to.HasValue)
                sessionsQuery = sessionsQuery.Where(s => s.Date <= to.Value);

            var sessionIds = sessionsQuery.Select(s => s.Id);

            var attendancesQuery = _context.Attendances
                .Where(a => sessionIds.Contains(a.SessionId));

            ViewBag.TotalSessions = sessionsQuery.Count();
            ViewBag.TotalAttendances = attendancesQuery.Count();

            ViewBag.ByStatus = attendancesQuery
                .GroupBy(a => a.Status)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToList();

            ViewBag.ByGrade = sessionsQuery
                .GroupJoin(
                    attendancesQuery,
                    s => s.Id,
                    a => a.SessionId,
                    (s, a) => new { s.Grade, Count = a.Count() }
                )
                .GroupBy(x => x.Grade)
                .Select(g => new
                {
                    Grade = g.Key,
                    Count = g.Sum(x => x.Count)
                })
                .OrderBy(x => x.Grade)
                .ToList();

            ViewBag.BySection = sessionsQuery
                .GroupJoin(
                    attendancesQuery,
                    s => s.Id,
                    a => a.SessionId,
                    (s, a) => new { s.Section, Count = a.Count() }
                )
                .GroupBy(x => x.Section)
                .Select(g => new
                {
                    Section = g.Key,
                    Count = g.Sum(x => x.Count)
                })
                .ToList();

            ViewBag.SelectedGrade = grade;
            ViewBag.SelectedSection = section;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            ViewBag.HasResults = attendancesQuery.Any();

            return View();
        }

        // Export filtered attendance data to Excel
        public IActionResult ExportToExcel(int? grade, string? section, DateTime? from, DateTime? to)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Teacher")
                return Unauthorized();

            var teacherId = HttpContext.Session.GetInt32("UserId");
            if (teacherId == null)
                return RedirectToAction("Login", "Auth");

            var sessionsQuery = _context.Sessions
                .Where(s => s.TeacherId == teacherId.Value)
                .AsQueryable();

            if (grade.HasValue)
                sessionsQuery = sessionsQuery.Where(s => s.Grade == grade.Value);

            if (!string.IsNullOrEmpty(section))
                sessionsQuery = sessionsQuery.Where(s => s.Section == section);

            if (from.HasValue)
                sessionsQuery = sessionsQuery.Where(s => s.Date >= from.Value);

            if (to.HasValue)
                sessionsQuery = sessionsQuery.Where(s => s.Date <= to.Value);

            var sessionIds = sessionsQuery.Select(s => s.Id);

            var attendances = _context.Attendances
                .Include(a => a.Session)
                .ThenInclude(s => s.Course)
                .Include(a => a.User)
                .Where(a => sessionIds.Contains(a.SessionId))
                .OrderBy(a => a.Session!.Date)
                .ToList();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Attendances");

                ws.Cells[1, 1].Value = "Date";
                ws.Cells[1, 2].Value = "Grade";
                ws.Cells[1, 3].Value = "Section";
                ws.Cells[1, 4].Value = "Course";
                ws.Cells[1, 5].Value = "Student";
                ws.Cells[1, 6].Value = "Status";
                ws.Cells[1, 7].Value = "Time Recorded";

                int row = 2;

                foreach (var a in attendances)
                {
                    ws.Cells[row, 1].Value = a.Session?.Date.ToString("yyyy-MM-dd");
                    ws.Cells[row, 2].Value = a.Session?.Grade;
                    ws.Cells[row, 3].Value = a.Session?.Section;
                    ws.Cells[row, 4].Value = a.Session?.Course?.Name;
                    ws.Cells[row, 5].Value = a.User?.FullName ?? "";
                    ws.Cells[row, 6].Value = a.Status.ToString();
                    ws.Cells[row, 7].Value = a.TimeRecorded?.ToString();
                    row++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var fileName = "Attendance.xlsx";

                return File(stream.ToArray(), contentType, fileName);
            }
        }

        // ================= STUDENT =================
        public IActionResult Student(DateTime? from, DateTime? to, int? courseId)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Student")
                return Unauthorized();

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return NotFound();

            ViewBag.Email = user.Email;

            var attendancesQuery = _context.Attendances
                .Include(a => a.Session)
                .ThenInclude(s => s.Course)
                .Where(a => a.UserId == userId.Value)
                .AsQueryable();

            if (from.HasValue)
                attendancesQuery = attendancesQuery.Where(a => a.Session!.Date >= from.Value);

            if (to.HasValue)
                attendancesQuery = attendancesQuery.Where(a => a.Session!.Date <= to.Value);

            if (courseId.HasValue)
                attendancesQuery = attendancesQuery.Where(a => a.Session!.CourseId == courseId.Value);

            ViewBag.Courses = _context.Courses.ToList();

            int totalAttendances = attendancesQuery.Count();
            ViewBag.TotalAttendances = totalAttendances;

            ViewBag.ByStatus = attendancesQuery
                .GroupBy(a => a.Status)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToList();

            ViewBag.ByCourse = attendancesQuery
                .GroupBy(a => a.Session!.Course!.Name)
                .Select(g => new
                {
                    Course = g.Key,
                    Count = g.Count()
                })
                .ToList();

            ViewBag.Recent = attendancesQuery
                .OrderByDescending(a => a.TimeRecorded)
                .Take(5)
                .Select(a => new
                {
                    Course = a.Session!.Course!.Name,
                    Status = a.Status.ToString(),
                    TimeRecorded = a.TimeRecorded
                })
                .ToList();

            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            ViewBag.SelectedCourse = courseId;
            ViewBag.HasResults = attendancesQuery.Any();

            return View();
        }

        // ================= PARENT =================
        public IActionResult Parent(DateTime? from, DateTime? to, int? courseId)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Parent")
                return Unauthorized();

            var parentId = HttpContext.Session.GetInt32("UserId");
            if (parentId == null)
                return Unauthorized();

            var child = _context.Users
                .FirstOrDefault(u => u.ParentId == parentId.Value && u.Role == "Student");

            if (child == null)
            {
                ViewBag.ChildName = "No linked student";
                ViewBag.Courses = _context.Courses.ToList();
                ViewBag.TotalAttendances = 0;
                ViewBag.ByStatus = Array.Empty<object>();
                ViewBag.ByCourse = Array.Empty<object>();
                ViewBag.Recent = Array.Empty<object>();
                ViewBag.From = from?.ToString("yyyy-MM-dd");
                ViewBag.To = to?.ToString("yyyy-MM-dd");
                ViewBag.SelectedCourse = courseId;
                ViewBag.HasResults = false;

                return View();
            }

            ViewBag.ChildName = child.FullName;

            var attendancesQuery = _context.Attendances
                .Include(a => a.Session)
                .ThenInclude(s => s.Course)
                .Where(a => a.UserId == child.Id)
                .AsQueryable();

            if (from.HasValue)
                attendancesQuery = attendancesQuery.Where(a => a.Session!.Date >= from.Value);

            if (to.HasValue)
                attendancesQuery = attendancesQuery.Where(a => a.Session!.Date <= to.Value);

            if (courseId.HasValue)
                attendancesQuery = attendancesQuery.Where(a => a.Session!.CourseId == courseId.Value);

            ViewBag.Courses = _context.Courses.ToList();

            int totalAttendances = attendancesQuery.Count();
            ViewBag.TotalAttendances = totalAttendances;

            var byStatus = attendancesQuery
                .GroupBy(a => a.Status)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToList();

            ViewBag.ByStatus = byStatus;

            ViewBag.ByCourse = attendancesQuery
                .GroupBy(a => a.Session!.Course!.Name)
                .Select(g => new
                {
                    Course = g.Key,
                    Count = g.Count()
                })
                .ToList();

            ViewBag.Recent = attendancesQuery
                .OrderByDescending(a => a.TimeRecorded)
                .Take(5)
                .Select(a => new
                {
                    Course = a.Session!.Course!.Name,
                    Status = a.Status.ToString(),
                    TimeRecorded = a.TimeRecorded
                })
                .ToList();

            int present = 0;
            int absent = 0;
            int late = 0;

            foreach (var item in byStatus)
            {
                if (item.Status == "Present") present = item.Count;
                if (item.Status == "Absent") absent = item.Count;
                if (item.Status == "Late") late = item.Count;
            }

            double percentage = totalAttendances == 0
                ? 0
                : (double)present / totalAttendances * 100;

            string riskLevel;
            string riskMessage;

            if (percentage < 75 || absent >= 5)
            {
                riskLevel = "High";
                riskMessage = "Attendance is critically low.";
            }
            else if (percentage < 85 || late >= 3)
            {
                riskLevel = "Warning";
                riskMessage = "Attendance needs attention.";
            }
            else
            {
                riskLevel = "Good";
                riskMessage = "Attendance is in a healthy range.";
            }

            ViewBag.RiskLevel = riskLevel;
            ViewBag.RiskMessage = riskMessage;
            ViewBag.AttendancePercentage = percentage.ToString("0");

            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            ViewBag.SelectedCourse = courseId;
            ViewBag.HasResults = attendancesQuery.Any();

            return View();
        }
    }
}