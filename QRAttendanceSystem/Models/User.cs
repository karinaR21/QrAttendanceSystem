namespace QRAttendanceSystem.Models
{
    public class User
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int? ParentId { get; set; }
        public User? Parent { get; set; }
        public ICollection<Student> Children { get; set; }
            = new List<Student>();

    }
}
