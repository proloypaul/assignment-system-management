using AssignmentSystem.Application.Features.Submissions.DTOs;
using AssignmentSystem.Application.Features.Submissions.Interfaces;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Infrastructure.Services;

public class SubmissionService : ISubmissionService
{
    private readonly ApplicationDbContext _db;

    public SubmissionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<SubmitResultDto> SubmitAsync(Guid assignmentId, Guid studentId, SubmitRequest request, string baseUrl)
    {
        var assignment = await _db.Assignments.FindAsync(assignmentId)
            ?? throw new KeyNotFoundException("Assignment not found.");

        if (assignment.Status != AssignmentStatus.Published)
            throw new InvalidOperationException("Assignment is not published.");

        if (DateTime.UtcNow > assignment.EndDate)
            throw new InvalidOperationException("Submission deadline has passed.");

        var existing = await _db.Submissions
            .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);

        if (existing is not null)
            throw new InvalidOperationException("You have already submitted for this assignment.");

        string? attachmentUrl = null;
        if (request.File is not null)
        {
            if (request.File.ContentType != "application/pdf")
                throw new ArgumentException("Only PDF files are allowed.");

            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "submissions");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(request.File.FileName)}";
            var filePath = Path.Combine(uploadPath, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            attachmentUrl = $"{baseUrl}/uploads/submissions/{fileName}";
        }

        var submission = new Submission
        {
            AssignmentId = assignmentId,
            StudentId = studentId,
            AnswerText = request.AnswerText,
            AttachmentFileUrl = attachmentUrl,
            SubmittedAt = DateTime.UtcNow,
            Status = SubmissionStatus.Submitted
        };

        _db.Submissions.Add(submission);
        await _db.SaveChangesAsync();

        return new SubmitResultDto
        {
            SubmissionId = submission.Id,
            Status = submission.Status.ToString(),
            AttachmentUrl = attachmentUrl
        };
    }

    public async Task<IEnumerable<MySubmissionDto>> GetMySubmissionsAsync(Guid studentId)
    {
        return await _db.Submissions
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new MySubmissionDto
            {
                Id = s.Id,
                AssignmentId = s.AssignmentId,
                AssignmentTitle = s.Assignment != null ? s.Assignment.Title : null,
                SubjectName = s.Assignment != null && s.Assignment.Subject != null ? s.Assignment.Subject.Name : null,
                CourseName = s.Assignment != null && s.Assignment.Subject != null && s.Assignment.Subject.Course != null ? s.Assignment.Subject.Course.Name : null,
                AnswerText = s.AnswerText,
                AttachmentFileUrl = s.AttachmentFileUrl,
                Status = s.Status.ToString(),
                MarksAwarded = s.MarksAwarded,
                Feedback = s.Feedback,
                SubmittedAt = s.SubmittedAt
            })
            .ToListAsync();
    }

    public async Task<MySubmissionDto?> GetMySubmissionAsync(Guid assignmentId, Guid studentId)
    {
        return await _db.Submissions
            .Where(s => s.AssignmentId == assignmentId && s.StudentId == studentId)
            .Select(s => new MySubmissionDto
            {
                Id = s.Id,
                AssignmentId = s.AssignmentId,
                AssignmentTitle = s.Assignment != null ? s.Assignment.Title : null,
                SubjectName = s.Assignment != null && s.Assignment.Subject != null ? s.Assignment.Subject.Name : null,
                CourseName = s.Assignment != null && s.Assignment.Subject != null && s.Assignment.Subject.Course != null ? s.Assignment.Subject.Course.Name : null,
                AnswerText = s.AnswerText,
                AttachmentFileUrl = s.AttachmentFileUrl,
                Status = s.Status.ToString(),
                MarksAwarded = s.MarksAwarded,
                Feedback = s.Feedback,
                SubmittedAt = s.SubmittedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task GradeAsync(Guid submissionId, GradeRequest request)
    {
        var submission = await _db.Submissions.FindAsync(submissionId)
            ?? throw new KeyNotFoundException("Submission not found.");

        submission.MarksAwarded = request.MarksAwarded;
        submission.Feedback = request.Feedback;
        submission.Status = SubmissionStatus.Graded;

        await _db.SaveChangesAsync();
    }
}
