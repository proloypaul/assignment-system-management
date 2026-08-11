using AssignmentSystem.Domain.Common;

namespace AssignmentSystem.Domain.Entities;

public class Subject : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Credits { get; set; }
    public string? SyllabusUrl { get; set; }
    public Guid CourseId { get; set; }

    // Navigation properties
    public Course Course { get; set; } = null!;
    public ICollection<TeacherSubjectAssignment> Teachers { get; set; } = new List<TeacherSubjectAssignment>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
