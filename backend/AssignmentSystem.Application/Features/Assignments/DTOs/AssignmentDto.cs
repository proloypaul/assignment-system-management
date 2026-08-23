namespace AssignmentSystem.Application.Features.Assignments.DTOs;

public class AssignmentDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxMarks { get; set; }
    public string Status { get; set; } = string.Empty;
    public AssignmentSubjectDto? Subject { get; set; }
    public AssignmentTeacherDto? Teacher { get; set; }
    public bool IsSubmitted { get; set; }
}

public class AssignmentSubjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AssignmentTeacherDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

public class SubmissionDto
{
    public Guid Id { get; set; }
    public Guid AssignmentId { get; set; }
    public string? AssignmentTitle { get; set; }
    public string? TeacherName { get; set; }
    public string? SubjectName { get; set; }
    public Guid StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? StudentEmail { get; set; }
    public string? AnswerText { get; set; }
    public string? AttachmentFileUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? MarksAwarded { get; set; }
    public string? Feedback { get; set; }
    public DateTime SubmittedAt { get; set; }
}
