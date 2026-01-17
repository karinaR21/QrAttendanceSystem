namespace QRAttendanceSystem.Models
{
    public class Session
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; }
        public string Title { get; set; }
    }
}
