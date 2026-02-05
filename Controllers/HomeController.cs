using Microsoft.AspNetCore.Mvc;

namespace QRAttendanceSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // НЯМА redirect-и тук
            // View-то решава кое partial да покаже
            return View();
        }

        public IActionResult _StudentHome()
        {
            if (HttpContext.Session.GetString("Role") != "Student")
                return RedirectToAction("Index");

            return View();
        }

        public IActionResult _TeacherHome()
        {
            if (HttpContext.Session.GetString("Role") != "Teacher")
                return RedirectToAction("Index");

            return View();
        }

        public IActionResult _ParentHome()
        {
            if (HttpContext.Session.GetString("Role") != "Parent")
                return RedirectToAction("Index");

            return View();
        }
    }
}
