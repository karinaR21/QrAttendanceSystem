using QRAttendanceSystem.Models;

public class Student
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public ICollection<Attendance> Attendances { get; set; }
        = new List<Attendance>();
}
