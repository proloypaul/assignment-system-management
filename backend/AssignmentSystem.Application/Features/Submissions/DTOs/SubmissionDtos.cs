using Microsoft.AspNetCore.Http;

namespace AssignmentSystem.Application.Features.Submissions.DTOs;

public class MySubmissionDto
{
    public Guid Id { get; set; }
    public Guid AssignmentId { get; set; }
    public string? AssignmentTitle { get; set; }
    public string? SubjectName { get; set; }
    public string? CourseName { get; set; }
    public string? AnswerText { get; set; }
    public string? AttachmentFileUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? MarksAwarded { get; set; }
    public string? Feedback { get; set; }
    public DateTime SubmittedAt { get; set; }
}

public class SubmitResultDto
{
    public Guid SubmissionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
}

public class SubmitRequest
{
    public string? AnswerText { get; set; }
    public IFormFile? File { get; set; }
}

public record GradeRequest(int MarksAwarded, string? Feedback);
