using Microsoft.EntityFrameworkCore;
using QRAttendanceSystem.Models;

namespace QRAttendanceSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<QrToken> QrTokens { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Absence> Absences { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Course>().HasData(
    new Course { Id = 1, Name = "Mathematics" },
    new Course { Id = 2, Name = "Bulgarian Language" },
    new Course { Id = 3, Name = "English Language" },
    new Course { Id = 4, Name = "Information Technologies" },
    new Course { Id = 5, Name = "History" }
);

            

        }






    }
}
