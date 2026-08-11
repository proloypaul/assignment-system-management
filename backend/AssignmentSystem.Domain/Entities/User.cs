using AssignmentSystem.Domain.Common;
using AssignmentSystem.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace AssignmentSystem.Domain.Entities;

/// <summary>
/// Application user extending ASP.NET Core Identity for secure authentication.
/// Identity handles password hashing, lockout, and security tokens.
/// </summary>
public class User : IdentityUser<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
    public ICollection<TeacherSubjectAssignment> SubjectAssignments { get; set; } = new List<TeacherSubjectAssignment>();
    public ICollection<Assignment> CreatedAssignments { get; set; } = new List<Assignment>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
