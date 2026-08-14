namespace AssignmentSystem.Domain.Entities;

/// <summary>
/// Explicit join entity for the many-to-many relationship between Teachers and Subjects.
/// Allows future extension with business metadata (e.g., AssignedDate, IsActive).
/// </summary>
public class TeacherSubjectAssignment
{
    public Guid TeacherId { get; set; }
    public Guid SubjectId { get; set; }
    public DateTime AssignedDate { get; set; }

    // Navigation properties
    public User Teacher { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
}
