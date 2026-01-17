using Microsoft.AspNetCore.Mvc;
using QRAttendanceSystem.Data;
using QRAttendanceSystem.Models;

public class SessionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public SessionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Courses = _context.Courses.ToList();
        return View();
    }

    [HttpPost]
    public IActionResult Create(Session session)
    {
        var course = _context.Courses.Find(session.CourseId);
        if (course == null)
            return Content("Invalid course");

        session.Title = $"{course.Name} - {session.Date:dd.MM.yyyy HH:mm}";

        _context.Sessions.Add(session);
        _context.SaveChanges();

        return RedirectToAction(
            "ShowQr",
            "Attendance",
            new { sessionId = session.Id }
        );
    }
}
