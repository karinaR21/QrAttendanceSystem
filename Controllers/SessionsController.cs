using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

    public IActionResult Index()
    {
        var sessions = _context.Sessions
            .Include(s => s.Course)
            .OrderByDescending(s => s.StartTime)
            .ToList();

        return View(sessions);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var vm = new CreateSessionViewModel
        {
            Courses = _context.Courses
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToList(),

            SessionDate = DateTime.Today
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult Create(CreateSessionViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Courses = _context.Courses
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToList();

            return View(vm);
        }

        var start = vm.SessionDate!.Value.Date
                    .Add(vm.StartTime!.Value);

        var session = new Session
        {
            CourseId = vm.CourseId!.Value,

            StartTime = start,
            PresentUntil = start.AddMinutes(3),
            EndTime = start.AddMinutes(45),

            Date = start.Date,
            Grade = vm.Grade!.Value,
            Section = vm.Section!
        };

        _context.Sessions.Add(session);
        _context.SaveChanges();

        return RedirectToAction(
            "Generate",
            "Attendance",
            new { sessionId = session.Id }
        );
    }

}
