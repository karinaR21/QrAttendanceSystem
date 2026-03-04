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
        // invalidate previous unused tokens
        var oldTokens = _context.QrTokens
            .Where(t => t.SessionId == sessionId && !t.IsUsed);

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
            .First(t => t.Id == token.Id);

        return View("ShowQr", token);
    }

    // ================= STUDENT =================

    [HttpGet]
    public IActionResult Scan()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register([FromBody] ScanRequest request)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
            return Unauthorized();

        var qr = _context.QrTokens
            .FirstOrDefault(q => q.Token == request.Token);

        if (qr == null ||
            qr.IsUsed ||
            qr.ExpirationTime < DateTime.UtcNow)
        {
            return BadRequest("QR expired or invalid");
        }

        var session = _context.Sessions
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
        else
        {
            status = AttendanceStatus.Late;
        }

        _context.Attendances.Add(new Attendance
        {
            UserId = userId.Value,
            SessionId = session.Id,
            TimeRecorded = now,
            Status = status
        });

        // mark QR as used
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
        var list = _context.Attendances
            .Where(a => a.SessionId == sessionId)
            .OrderBy(a => a.TimeRecorded)
            .Select(a => new
            {
                Student = a.User.FullName,
                Status = a.Status.ToString(),
                Time = a.TimeRecorded.HasValue
    ? a.TimeRecorded.Value.ToLocalTime().ToString("HH:mm:ss")
    : "-"
            })
            .ToList();

        return Json(list);
    }

}