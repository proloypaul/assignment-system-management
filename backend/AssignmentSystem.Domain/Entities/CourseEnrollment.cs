namespace AssignmentSystem.Domain.Entities;

/// <summary>
/// Explicit join entity for the many-to-many relationship between Students and Courses.
/// Allows future extension with enrollment metadata (e.g., EnrolledAt, Grade, Status).
/// </summary>
public class CourseEnrollment
{
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public DateTime EnrolledAt { get; set; }

    // Navigation properties
    public User Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
