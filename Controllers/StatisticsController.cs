using Microsoft.AspNetCore.Mvc;
using QRAttendanceSystem.Data;
using Microsoft.EntityFrameworkCore;
using QRAttendanceSystem.Models;

public class StatisticsController : Controller
{
    private readonly ApplicationDbContext _context;

    public StatisticsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ===== ADMIN / TEACHER =====
    public IActionResult Index()
    {
        var totalSessions = _context.Sessions.Count();
        var totalAttendances = _context.Attendances.Count();

        var attendanceByStatus = _context.Attendances
            .GroupBy(a => a.Status)
            .Select(g => new
            {
                Status = g.Key.ToString(),
                Count = g.Count()
            })
            .ToList();

        var attendanceByGrade = _context.Sessions
            .GroupJoin(
                _context.Attendances,
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

        var attendanceBySection = _context.Sessions
            .GroupJoin(
                _context.Attendances,
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

        ViewBag.TotalSessions = totalSessions;
        ViewBag.TotalAttendances = totalAttendances;
        ViewBag.ByGrade = attendanceByGrade;
        ViewBag.BySection = attendanceBySection;
        ViewBag.ByStatus = attendanceByStatus;

        return View();
    }

    // ===== STUDENT =====
    public IActionResult Student()
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Student")
            return Unauthorized();

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Unauthorized();

        // 🔥 ВЗИМАМЕ УЧЕНИКА
        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
            return NotFound();

        // 🔥 EMAIL ЗА HEADER
        ViewBag.Email = user.Email;

        ViewBag.TotalAttendances = _context.Attendances
            .Count(a => a.UserId == userId);

        ViewBag.ByStatus = _context.Attendances
            .Where(a => a.UserId == userId)
            .GroupBy(a => a.Status)
            .Select(g => new
            {
                Status = g.Key.ToString(),
                Count = g.Count()
            })
            .ToList();

        ViewBag.ByCourse = _context.Attendances
            .Where(a => a.UserId == userId)
            .GroupBy(a => a.Session.Course.Name)
            .Select(g => new
            {
                Course = g.Key,
                Count = g.Count()
            })
            .ToList();

        ViewBag.Recent = _context.Attendances
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.TimeRecorded)
            .Take(5)
            .Select(a => new
            {
                Course = a.Session.Course.Name,
                Status = a.Status.ToString(),
                TimeRecorded = a.TimeRecorded
            })
            .ToList();

        return View();
    }

    // ===== PARENT =====
    public IActionResult Parent()
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

        ViewBag.TotalAttendances = _context.Attendances
            .Count(a => a.UserId == child.Id);

        ViewBag.ByStatus = _context.Attendances
            .Where(a => a.UserId == child.Id)
            .GroupBy(a => a.Status)
            .Select(g => new
            {
                Status = g.Key.ToString(),
                Count = g.Count()
            })
            .ToList();

        ViewBag.ByCourse = _context.Attendances
            .Where(a => a.UserId == child.Id)
            .GroupBy(a => a.Session.Course.Name)
            .Select(g => new
            {
                Course = g.Key,
                Count = g.Count()
            })
            .ToList();

        ViewBag.Recent = _context.Attendances
            .Where(a => a.UserId == child.Id)
            .OrderByDescending(a => a.TimeRecorded)
            .Take(5)
            .Select(a => new
            {
                Course = a.Session.Course.Name,
                Status = a.Status.ToString(),
                TimeRecorded = a.TimeRecorded
            })
            .ToList();

        return View();
    }
}
