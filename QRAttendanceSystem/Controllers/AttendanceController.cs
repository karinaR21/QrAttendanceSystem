using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRAttendanceSystem.Data;
using QRAttendanceSystem.Models;

[Route("[controller]/[action]")]
public class AttendanceController : Controller
{
    private readonly ApplicationDbContext _context;

    public AttendanceController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ================= TEACHER =================

    [HttpGet]
    public IActionResult Generate(int sessionId)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Teacher")
            return Unauthorized();

        var teacherId = HttpContext.Session.GetInt32("UserId");
        if (teacherId == null)
            return RedirectToAction("Login", "Auth");

        var session = _context.Sessions
            .Include(s => s.Course)
            .FirstOrDefault(s => s.Id == sessionId && s.TeacherId == teacherId.Value);

        if (session == null)
            return Unauthorized();

        var oldTokens = _context.QrTokens
            .Where(t => t.SessionId == sessionId && !t.IsUsed)
            .ToList();

        foreach (var t in oldTokens)
        {
            t.ExpirationTime = DateTime.UtcNow;
        }

        var token = new QrToken
        {
            Token = Guid.NewGuid().ToString(),
            ExpirationTime = DateTime.UtcNow.AddSeconds(15),
            IsUsed = false,
            SessionId = sessionId
        };

        _context.QrTokens.Add(token);
        _context.SaveChanges();

        token = _context.QrTokens
            .Include(t => t.Session)
            .ThenInclude(s => s.Course)
            .First(t => t.Id == token.Id);

        return View("ShowQr", token);
    }

    // ================= STUDENT =================

    [HttpGet]
    public IActionResult Scan()
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Student")
            return Unauthorized();

        return View();
    }

    [HttpPost]
    public IActionResult Register([FromBody] ScanRequest request)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Student")
            return Unauthorized();

        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Unauthorized();

        if (request == null || string.IsNullOrWhiteSpace(request.Token))
            return BadRequest("Invalid QR request");

        var qr = _context.QrTokens
            .FirstOrDefault(q => q.Token == request.Token);

        if (qr == null || qr.IsUsed || qr.ExpirationTime < DateTime.UtcNow)
        {
            return BadRequest("QR expired or invalid");
        }

        var session = _context.Sessions
            .Include(s => s.Course)
            .FirstOrDefault(s => s.Id == qr.SessionId);

        if (session == null)
            return BadRequest("Invalid session");

        var now = DateTime.UtcNow;

        bool exists = _context.Attendances.Any(a =>
            a.UserId == userId.Value &&
            a.SessionId == session.Id);

        if (exists)
            return BadRequest("Attendance already recorded");

        AttendanceStatus status;

        if (now > session.EndTime)
        {
            status = AttendanceStatus.Absent;
        }
        else if (now <= session.PresentUntil)
        {
            status = AttendanceStatus.Present;
        }
        else if (now <= session.LateUntil)
        {
            status = AttendanceStatus.Late;
        }
        else
        {
            status = AttendanceStatus.Absent;
        }

        _context.Attendances.Add(new Attendance
        {
            UserId = userId.Value,
            SessionId = session.Id,
            TimeRecorded = now,
            Status = status
        });

        qr.IsUsed = true;

        _context.SaveChanges();

        return Ok(status.ToString());
    }

    [HttpGet]
    public IActionResult QrInvalid()
    {
        return View();
    }

    [HttpGet]
    public IActionResult LiveList(int sessionId)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Teacher")
            return Unauthorized();

        var teacherId = HttpContext.Session.GetInt32("UserId");
        if (teacherId == null)
            return Unauthorized();

        var sessionExists = _context.Sessions
            .Any(s => s.Id == sessionId && s.TeacherId == teacherId.Value);

        if (!sessionExists)
            return Unauthorized();

        var list = _context.Attendances
            .Include(a => a.User)
            .Where(a => a.SessionId == sessionId)
            .OrderBy(a => a.TimeRecorded)
            .Select(a => new
            {
                Student = a.User != null ? a.User.FullName : "",
                Status = a.Status.ToString(),
                Time = a.TimeRecorded.HasValue
                    ? a.TimeRecorded.Value.ToLocalTime().ToString("HH:mm:ss")
                    : "-"
            })
            .ToList();

        return Json(list);
    }
}