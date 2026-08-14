using AssignmentSystem.Domain.Common;
using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Domain.Entities;

public class Assignment : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxMarks { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Draft;
    public Guid TeacherId { get; set; }
    public Guid SubjectId { get; set; }

    // Navigation properties
    public User Teacher { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
