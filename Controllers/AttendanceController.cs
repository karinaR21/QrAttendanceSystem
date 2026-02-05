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

    // ====== TEACHER ======

    [HttpPost]
    public IActionResult GenerateQr(int sessionId)
    {
        var token = new QrToken
        {
            Token = Guid.NewGuid().ToString(),
            ExpirationTime = DateTime.MaxValue, // QR не изтича
            IsUsed = false,
            SessionId = sessionId
        };

        _context.QrTokens.Add(token);
        _context.SaveChanges();

        return RedirectToAction("Generate", new { sessionId });
    }

    [HttpGet]
    public IActionResult Generate(int sessionId)
    {
        var token = _context.QrTokens
            .Include(t => t.Session)   // 🔥 КЛЮЧОВО
            .OrderByDescending(t => t.Id)
            .FirstOrDefault(t => t.SessionId == sessionId);

        if (token == null)
        {
            token = new QrToken
            {
                Token = Guid.NewGuid().ToString(),
                ExpirationTime = DateTime.MaxValue,
                IsUsed = false,
                SessionId = sessionId
            };

            _context.QrTokens.Add(token);
            _context.SaveChanges();

            // презареждаме със Session
            token = _context.QrTokens
                .Include(t => t.Session)
                .First(t => t.Id == token.Id);
        }

        return View("ShowQr", token);
    }

    // ====== STUDENT ======

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

        if (qr == null)
            return BadRequest("Invalid QR");

        var session = _context.Sessions.FirstOrDefault(s => s.Id == qr.SessionId);
        if (session == null)
            return BadRequest("Invalid session");

        // 🔥 ВАЖНО: ЛОКАЛНО ВРЕМЕ
        var now = DateTime.Now;

        bool exists = _context.Attendances.Any(a =>
            a.UserId == userId.Value &&
            a.SessionId == session.Id);

        if (exists)
            return BadRequest("Attendance already recorded");

        AttendanceStatus status;

        // ❌ след 45 мин → отсъствие
        if (now > session.EndTime)
        {
            status = AttendanceStatus.Absent;
        }
        // 🟢 до 5 мин → присъствие
        else if (now <= session.PresentUntil)
        {
            status = AttendanceStatus.Present;
        }
        // 🟡 след 5 мин → закъснение
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
                    ? a.TimeRecorded.Value.ToString("HH:mm:ss")
                    : "-"
            })
            .ToList();

        return Json(list);
    }

}
