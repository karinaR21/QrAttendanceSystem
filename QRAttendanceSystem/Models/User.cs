public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Role { get; set; } = null!;
    public int? ParentId { get; set; }   // само за Student
    public User? Parent { get; set; }    // navigation property
}
