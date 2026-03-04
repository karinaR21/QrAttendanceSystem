using System.ComponentModel.DataAnnotations;

namespace QRAttendanceSystem.Models
{
    public enum AttendanceStatus
    {
        Present,
        Late,
        Absent
    }

    public class Attendance
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int SessionId { get; set; }
        public Session? Session { get; set; }

        public DateTime? TimeRecorded { get; set; }

        public AttendanceStatus Status { get; set; }
    }
}
