namespace QRAttendanceSystem.Models
{
    public class QrToken
    {
        public int Id { get; set; }

        public string Token { get; set; }

        public DateTime ExpirationTime { get; set; }

        public bool IsUsed { get; set; }

        public int SessionId { get; set; }
        public Session Session { get; set; }
    }
}
