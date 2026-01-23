using Microsoft.AspNetCore.Mvc;
using QRAttendanceSystem.Data;
using QRAttendanceSystem.Models;
using QRAttendanceSystem.ViewModels;

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
        return View(new CreateSessionViewModel()); //  ВАЖНО
    }

    
    [HttpPost]
    public IActionResult Create(CreateSessionViewModel model)
    {
        ViewBag.Courses = _context.Courses.ToList();

        if (!ModelState.IsValid)
        {
            return View(model); 
        }

        var course = _context.Courses.Find(model.CourseId!.Value);
        if (course == null)
        {
            ModelState.AddModelError("CourseId", "Invalid course");
            return View(model);
        }

        var session = new Session
        {
            CourseId = model.CourseId.Value,
            Date = model.Date.Value,
            Grade = model.Grade.Value,
            Section = model.Section,
            Title = $"{model.Grade}{model.Section} – {course.Name} – {model.Date:dd.MM.yyyy HH:mm}"
        };


        _context.Sessions.Add(session);
        _context.SaveChanges();

        return RedirectToAction(
            "ShowQr",
            "Attendance",
            new { sessionId = session.Id }
        );
    }
}
