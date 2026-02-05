using System.ComponentModel.DataAnnotations;

namespace QRAttendanceSystem.Models
{
    public class Session
    {
        public int Id { get; set; }

        public int CourseId { get; set; }
        public Course? Course { get; set; }

        public DateTime Date { get; set; }
        public int Grade { get; set; }
        public string Section { get; set; } = string.Empty;

        // ➕ ДОБАВЕНИ (без да се маха нищо съществуващо)
        public DateTime StartTime { get; set; }
        public DateTime PresentUntil { get; set; }
        public DateTime LateUntil { get; set; }
        public DateTime EndTime { get; set; }
    }
}
