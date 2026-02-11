using Microsoft.AspNetCore.Mvc;
using QRAttendanceSystem.Data;
using QRAttendanceSystem.Models;

namespace QRAttendanceSystem.Controllers
{
    public class AbsencesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AbsencesController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public IActionResult Create()
        {
            var parentEmail = HttpContext.Session.GetString("Email");

            var parent = _context.Users
                .FirstOrDefault(u => u.Email == parentEmail);

            if (parent == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var children = _context.Users
    .Where(u => u.ParentId == parent.Id && u.Role == "Student")
    .ToList();


            ViewBag.Children = children;

            return View();

        }

        [HttpPost]
        public async Task<IActionResult> Create(Absence model, IFormFile? document)
        {
            if (!ModelState.IsValid)
            {
                var parentEmail = HttpContext.Session.GetString("Email");

                var parent = _context.Users
                    .FirstOrDefault(u => u.Email == parentEmail);

                ViewBag.Children = _context.Users
                    .Where(u => u.ParentId == parent.Id && u.Role == "Student")
                    .ToList();

                return View(model);
            }


            if (document != null)
            {
                var folder = Path.Combine(_env.WebRootPath, "uploads");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var fileName = Guid.NewGuid() + Path.GetExtension(document.FileName);
                var path = Path.Combine(folder, fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await document.CopyToAsync(stream);

                model.DocumentPath = "/uploads/" + fileName;
            }

            _context.Absences.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Parent");
        }
    }

}
