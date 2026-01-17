using Microsoft.AspNetCore.Mvc;
using QRAttendanceSystem.Data;
using QRAttendanceSystem.Models;

public class AttendanceController : Controller
{
    private readonly ApplicationDbContext _context;

    public AttendanceController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Навигация от менюто (САМО за Teacher)
    public IActionResult GenerateQr()
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Teacher")
            return Unauthorized();

        return RedirectToAction("Create", "Sessions");
    }

    // Реално генериране на QR
    public IActionResult ShowQr(int sessionId)
    {
        var session = _context.Sessions.Find(sessionId);
        if (session == null)
            return RedirectToAction("Create", "Sessions");

        var token = new QrToken
        {
            Token = Guid.NewGuid().ToString(),
            SessionId = session.Id,
            ExpirationTime = DateTime.Now.AddMinutes(5),
            IsUsed = false
        };

        _context.QrTokens.Add(token);
        _context.SaveChanges();

        return View(token);
    }
    [HttpGet]
    public IActionResult Scan()
    {
        var role = HttpContext.Session.GetString("Role");

        if (role != "Student")
            return Unauthorized();

        return View();
    }


    // 📱 СКАНИРАНЕ ОТ ТЕЛЕФОН (Student)
    [HttpPost]
    public IActionResult Register([FromBody] ScanRequest request)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Student")
            return Unauthorized("Only students can scan");

        var studentId = HttpContext.Session.GetInt32("StudentId");
        if (studentId == null)
            return Unauthorized("Not logged in");

        var qr = _context.QrTokens.FirstOrDefault(q =>
            q.Token == request.Token &&
            q.ExpirationTime > DateTime.Now &&
            !q.IsUsed);

        if (qr == null)
            return BadRequest("Invalid or expired QR code");

        bool alreadyExists = _context.Attendances.Any(a =>
            a.StudentId == studentId.Value &&
            a.SessionId == qr.SessionId);

        if (alreadyExists)
            return BadRequest("Attendance already recorded");

        var attendance = new Attendance
        {
            StudentId = studentId.Value,
            SessionId = qr.SessionId,
            TimeRecorded = DateTime.Now
        };

        qr.IsUsed = true;

        _context.Attendances.Add(attendance);
        _context.SaveChanges();

        return Ok("Attendance recorded successfully");
    }
}
