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
    public IActionResult Index(int? grade, string? section,
                          DateTime? from, DateTime? to)
    {
        var sessionsQuery = _context.Sessions.AsQueryable();

        // ✅ FILTERS
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


        // ===== TOTALS =====

        ViewBag.TotalSessions = sessionsQuery.Count();
        ViewBag.TotalAttendances = attendancesQuery.Count();


        // ===== STATUS =====

        ViewBag.ByStatus = attendancesQuery
            .GroupBy(a => a.Status)
            .Select(g => new
            {
                Status = g.Key.ToString(),
                Count = g.Count()
            })
            .ToList();


        // ===== GRADE =====

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


        // ===== SECTION =====

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

        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
            return NotFound();

        ViewBag.Email = user.Email;

       

        var totalSessions = _context.Attendances
    .Where(a => a.UserId == userId)
    .Select(a => a.SessionId)
    .Distinct()
    .Count();

        

        var attendancesQuery = _context.Attendances
            .Where(a => a.UserId == userId);

        var totalAttendances = attendancesQuery.Count();


        double attendanceRate = totalSessions == 0
            ? 0
            : Math.Round((double)totalAttendances * 100 / totalSessions, 0);

       

        ViewBag.TotalSessions = totalSessions;
        ViewBag.TotalAttendances = totalAttendances;
        ViewBag.AttendanceRate = attendanceRate;

        ViewBag.ByStatus = attendancesQuery
            .GroupBy(a => a.Status)
            .Select(g => new
            {
                Status = g.Key.ToString(),
                Count = g.Count()
            })
            .ToList();


        ViewBag.ByCourse = attendancesQuery
            .Include(a => a.Session)
            .ThenInclude(s => s.Course)
            .GroupBy(a => a.Session.Course.Name)
            .Select(g => new
            {
                Course = g.Key,
                Count = g.Count()
            })
            .ToList();

        

        ViewBag.Recent = attendancesQuery
            .Include(a => a.Session)
            .ThenInclude(s => s.Course)
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
