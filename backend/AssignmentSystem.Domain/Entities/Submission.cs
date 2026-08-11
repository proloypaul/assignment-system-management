using AssignmentSystem.Domain.Common;
using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Domain.Entities;

public class Submission : BaseEntity
{
    public Guid AssignmentId { get; set; }
    public Guid StudentId { get; set; }
    public string? AnswerText { get; set; }

    /// <summary>
    /// URL to the attachment file (PDF). Initially stored on local server,
    /// planned migration to cloud storage (e.g., S3, Azure Blob) in a later phase.
    /// </summary>
    public string? AttachmentFileUrl { get; set; }

    public DateTime SubmittedAt { get; set; }
    public int? MarksAwarded { get; set; }
    public string? Feedback { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

    // Navigation properties
    public Assignment Assignment { get; set; } = null!;
    public User Student { get; set; } = null!;
}
