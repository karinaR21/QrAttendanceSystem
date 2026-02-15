using Microsoft.AspNetCore.Mvc;
using QRAttendanceSystem.Data;
using Microsoft.EntityFrameworkCore;
using QRAttendanceSystem.Models;
using OfficeOpenXml;

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
        // RETURN SELECTED FILTERS
        ViewBag.SelectedGrade = grade;
        ViewBag.SelectedSection = section;
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.HasResults = attendancesQuery.Any();

        return View();
    }


    // ===== STUDENT =====
    // ===== STUDENT =====
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


        // ✅ BASE QUERY
        var attendancesQuery = _context.Attendances
            .Include(a => a.Session)
            .ThenInclude(s => s.Course)
            .Where(a => a.UserId == userId)
            .AsQueryable();


        // ================= FILTERS =================

        if (from.HasValue)
            attendancesQuery = attendancesQuery
                .Where(a => a.Session.Date >= from.Value);

        if (to.HasValue)
            attendancesQuery = attendancesQuery
                .Where(a => a.Session.Date <= to.Value);

        if (courseId.HasValue)
            attendancesQuery = attendancesQuery
                .Where(a => a.Session.CourseId == courseId);


        // ================= DROPDOWN DATA =================

        ViewBag.Courses = _context.Courses.ToList();


        // ================= TOTALS =================

        var totalSessions = attendancesQuery
            .Select(a => a.SessionId)
            .Distinct()
            .Count();

        var totalAttendances = attendancesQuery.Count();


        double attendanceRate = totalSessions == 0
            ? 0
            : Math.Round((double)totalAttendances * 100 / totalSessions, 0);


        ViewBag.TotalSessions = totalSessions;
        ViewBag.TotalAttendances = totalAttendances;
        ViewBag.AttendanceRate = attendanceRate;


        // ================= STATUS =================

        ViewBag.ByStatus = attendancesQuery
            .GroupBy(a => a.Status)
            .Select(g => new
            {
                Status = g.Key.ToString(),
                Count = g.Count()
            })
            .ToList();


        // ================= COURSE =================

        ViewBag.ByCourse = attendancesQuery
            .GroupBy(a => a.Session.Course.Name)
            .Select(g => new
            {
                Course = g.Key,
                Count = g.Count()
            })
            .ToList();


        // ================= RECENT =================

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


        // ================= FILTER STATE =================

        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.SelectedCourse = courseId;

        ViewBag.HasResults = attendancesQuery.Any();


        return View();
    }



    // ===== PARENT =====
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


        // ⭐ SUPER IMPORTANT → IQueryable
        var attendancesQuery = _context.Attendances
            .Include(a => a.Session)
            .ThenInclude(s => s.Course)
            .Where(a => a.UserId == child.Id)
            .AsQueryable();



        // ✅ FILTERS
        if (from.HasValue)
            attendancesQuery = attendancesQuery
                .Where(a => a.Session.Date >= from.Value);

        if (to.HasValue)
            attendancesQuery = attendancesQuery
                .Where(a => a.Session.Date <= to.Value);

        if (courseId.HasValue)
            attendancesQuery = attendancesQuery
                .Where(a => a.Session.CourseId == courseId.Value);



        // Needed for dropdown
        ViewBag.Courses = _context.Courses.ToList();



        // ===== STATS =====

        ViewBag.TotalAttendances = attendancesQuery.Count();



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



        // ===== RETURN FILTER VALUES =====

        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.SelectedCourse = courseId;

        ViewBag.HasResults = attendancesQuery.Any();



        return View();
    }
    public IActionResult ExportToExcel(int? grade, string? section,
                                       DateTime? from, DateTime? to)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("Attendance");

            worksheet.Cells[1, 1].Value = "Grade";
            worksheet.Cells[1, 2].Value = "Section";
            worksheet.Cells[1, 3].Value = "Date";
            worksheet.Cells[1, 4].Value = "Course";

            var sessionsQuery = _context.Sessions.AsQueryable();

            if (grade.HasValue)
                sessionsQuery = sessionsQuery.Where(s => s.Grade == grade);

            if (!string.IsNullOrEmpty(section))
                sessionsQuery = sessionsQuery.Where(s => s.Section == section);

            if (from.HasValue)
                sessionsQuery = sessionsQuery.Where(s => s.Date >= from.Value);

            if (to.HasValue)
                sessionsQuery = sessionsQuery.Where(s => s.Date <= to.Value);

            var sessions = sessionsQuery
                .Include(s => s.Course)
                .ToList();

            int row = 2;

            foreach (var s in sessions)
            {
                worksheet.Cells[row, 1].Value = s.Grade;
                worksheet.Cells[row, 2].Value = s.Section;
                worksheet.Cells[row, 3].Value = s.Date.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 4].Value = s.Course?.Name;

                row++;
            }

            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "AttendanceExport.xlsx");
        }
    }

}