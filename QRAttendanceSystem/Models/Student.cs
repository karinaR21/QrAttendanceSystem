namespace QRAttendanceSystem.Models
{
    public class Student
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public int ParentId { get; set; }

        public User Parent { get; set; } = null!;

        public ICollection<Attendance> Attendances { get; set; }
            = new List<Attendance>();
    }
}
