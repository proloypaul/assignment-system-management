using AssignmentSystem.Domain.Common;

namespace AssignmentSystem.Domain.Entities;

public class Course : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }

    // Navigation properties
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
}
