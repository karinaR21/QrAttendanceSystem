using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QRAttendanceSystem.Data;
using QRAttendanceSystem.Models;

public class StatisticsController : Controller
{
    private readonly ApplicationDbContext _context;

    public StatisticsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ================= ADMIN / TEACHER =================
    public IActionResult Index(int? grade, string? section,
                              DateTime? from, DateTime? to)
    {
        var sessionsQuery = _context.Sessions.AsQueryable();

        if (grade.HasValue)
            sessionsQuery = sessionsQuery.Where(s => s.Grade == grade);

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
            .Where(a => a.UserId == userId)
            .AsQueryable();

        if (from.HasValue)
            attendancesQuery = attendancesQuery
                .Where(a => a.Session.Date >= from.Value);

        if (to.HasValue)
            attendancesQuery = attendancesQuery
                .Where(a => a.Session.Date <= to.Value);

        if (courseId.HasValue)
            attendancesQuery = attendancesQuery
                .Where(a => a.Session.CourseId == courseId);

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
            .GroupBy(a => a.Session.Course.Name)
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
                Course = a.Session.Course.Name,
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
            .FirstOrDefault(u => u.ParentId == parentId && u.Role == "Student");

        if (child == null)
            return NotFound();

        ViewBag.ChildName = child.FullName;

        var attendancesQuery = _context.Attendances
            .Include(a => a.Session)
            .ThenInclude(s => s.Course)
            .Where(a => a.UserId == child.Id)
            .AsQueryable();

        if (from.HasValue)
            attendancesQuery = attendancesQuery
                .Where(a => a.Session.Date >= from.Value);

        if (to.HasValue)
            attendancesQuery = attendancesQuery
                .Where(a => a.Session.Date <= to.Value);

        if (courseId.HasValue)
            attendancesQuery = attendancesQuery
                .Where(a => a.Session.CourseId == courseId.Value);

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
            .GroupBy(a => a.Session.Course.Name)
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
                Course = a.Session.Course.Name,
                Status = a.Status.ToString(),
                TimeRecorded = a.TimeRecorded
            })
            .ToList();

        // ===== RISK SYSTEM =====
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