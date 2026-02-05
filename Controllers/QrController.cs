using Microsoft.AspNetCore.Mvc;
using QRAttendanceSystem.Data;
using QRAttendanceSystem.Models;
using QRCoder;

namespace QRAttendanceSystem.Controllers
{
    public class QrController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QrController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Generate(int sessionId)
        {
            var sessionExists = _context.Sessions.Any(s => s.Id == sessionId);
            if (!sessionExists)
            {
                return BadRequest("Invalid session");
            }

            var token = Guid.NewGuid().ToString();

            var qrToken = new QrToken
            {
                Token = token,
                SessionId = sessionId,
                ExpirationTime = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false
            };

            _context.QrTokens.Add(qrToken);
            _context.SaveChanges();

            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(token, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(data);

            var bytes = qrCode.GetGraphic(20);
            return File(bytes, "image/png");
        }

    }
}
