using Microsoft.AspNetCore.Mvc;
using QRAttendanceSystem.Data;
using Microsoft.EntityFrameworkCore;

public class StatisticsController : Controller
{
    private readonly ApplicationDbContext _context;

    public StatisticsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var totalSessions = _context.Sessions.Count();
        var totalAttendances = _context.Attendances.Count();

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

        return View();
    }
    public IActionResult Student()
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Student")
            return Unauthorized();

        var studentId = HttpContext.Session.GetInt32("StudentId");
        if (studentId == null)
            return Unauthorized();

        var totalAttendances = _context.Attendances
            .Count(a => a.StudentId == studentId);

        var attendanceByCourse = _context.Attendances
            .Where(a => a.StudentId == studentId)
            .GroupBy(a => a.Session.Course.Name)
            .Select(g => new
            {
                Course = g.Key,
                Count = g.Count()
            })
            .ToList();

        var recentAttendances = _context.Attendances
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.TimeRecorded)
            .Take(5)
            .Select(a => new
            {
                a.Session.Title,
                a.TimeRecorded
            })
            .ToList();

        ViewBag.TotalAttendances = totalAttendances;
        ViewBag.ByCourse = attendanceByCourse;
        ViewBag.Recent = recentAttendances;

        return View();
    }
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
            return NotFound("No linked student");

        var totalAttendances = _context.Attendances
            .Count(a => a.StudentId == child.Id);

        var attendanceByCourse = _context.Attendances
            .Where(a => a.StudentId == child.Id)
            .GroupBy(a => a.Session.Course.Name)
            .Select(g => new
            {
                Course = g.Key,
                Count = g.Count()
            })
            .ToList();

        var recentAttendances = _context.Attendances
            .Where(a => a.StudentId == child.Id)
            .OrderByDescending(a => a.TimeRecorded)
            .Take(5)
            .Select(a => new
            {
                a.Session.Title,
                a.TimeRecorded
            })
            .ToList();

        ViewBag.ChildEmail = child.Email;
        ViewBag.TotalAttendances = totalAttendances;
        ViewBag.ByCourse = attendanceByCourse;
        ViewBag.Recent = recentAttendances;

        return View();
    }

}
