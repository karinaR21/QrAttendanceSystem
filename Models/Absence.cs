namespace QRAttendanceSystem.Models
{
    public class Absence
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User Student { get; set; } = null!;

        public DateTime AbsenceDate { get; set; }

        public string? Reason { get; set; }

        public string? DocumentPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";

    }

}
