

using System;

namespace QRAttendanceSystem.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        public DateTime TimeRecorded { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; }

        public int SessionId { get; set; }
        public Session Session { get; set; }
    }
}
