using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace QRAttendanceSystem.ViewModels
{
    public class CreateSessionViewModel
    {
        [Required(ErrorMessage = "Course is required")]
        public int? CourseId { get; set; }

        // ⭐ САМО ДАТА
        [Required(ErrorMessage = "Session date is required")]
        [DataType(DataType.Date)]
        public DateTime? SessionDate { get; set; }

        // ⭐ САМО ЧАС
        [Required(ErrorMessage = "Start time is required")]
        [DataType(DataType.Time)]
        public TimeSpan? StartTime { get; set; }

        [Required(ErrorMessage = "Grade is required")]
        [Range(7, 12, ErrorMessage = "Grade must be between 7 and 12")]
        public int? Grade { get; set; }

        [Required(ErrorMessage = "Section is required")]
        public string? Section { get; set; }

        public List<SelectListItem> Courses { get; set; } = new();
    }
}
