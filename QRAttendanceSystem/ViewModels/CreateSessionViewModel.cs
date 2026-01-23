using System.ComponentModel.DataAnnotations;

namespace QRAttendanceSystem.ViewModels
{
    public class CreateSessionViewModel
    {
        [Required(ErrorMessage = "Course is required")]
        public int? CourseId { get; set; }

        [Required(ErrorMessage = "Date and time is required")]
        public DateTime? Date { get; set; }

        [Required(ErrorMessage = "Grade is required")]
        [Range(7, 12, ErrorMessage = "Grade must be between 7 and 12")]
        public int? Grade { get; set; }

        [Required(ErrorMessage = "Section is required")]
        public string? Section { get; set; }

    }
}
