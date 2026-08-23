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
    private readonly ISubmissionQueue _queue;

    public SubmissionService(ApplicationDbContext db, ISubmissionQueue queue)
    {
        _db = db;
        _queue = queue;
    }

    /// <summary>
    /// Fast-path submit:
    ///   1. Validate (DB, deadline, duplicate).
    ///   2. Save file to a TEMP folder (quick — same disk, no network).
    ///   3. INSERT Submission with Status = Processing.
    ///   4. Enqueue a job for the BackgroundService to finish.
    ///   5. Return immediately — the HTTP response is sent before the file is fully processed.
    /// </summary>
    public async Task<SubmitResultDto> SubmitAsync(
        Guid assignmentId, Guid studentId, SubmitRequest request, string baseUrl)
    {
        // ── 1. Validate assignment ────────────────────────────────────────────
        var assignment = await _db.Assignments.FindAsync(assignmentId)
            ?? throw new KeyNotFoundException("Assignment not found.");

        if (assignment.Status != AssignmentStatus.Published)
            throw new InvalidOperationException("Assignment is not published.");

        if (DateTime.UtcNow > assignment.EndDate)
            throw new InvalidOperationException("Submission deadline has passed.");

        // ── 2. Check duplicate ───────────────────────────────────────────────
        var existing = await _db.Submissions
            .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);

        if (existing is not null)
            throw new InvalidOperationException("You have already submitted for this assignment.");

        // ── 3. Validate file type (if provided) ──────────────────────────────
        if (request.File is not null && request.File.ContentType != "application/pdf")
            throw new ArgumentException("Only PDF files are allowed.");

        // ── 4. Save file to TEMP location on disk ────────────────────────────
        string? tempFilePath = null;
        string? originalFileName = null;

        if (request.File is not null)
        {
            var tempDir = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot", "uploads", "temp");

            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);

            originalFileName = Path.GetFileName(request.File.FileName);
            tempFilePath = Path.Combine(tempDir, $"{Guid.NewGuid()}_{originalFileName}");

            await using var stream = new FileStream(tempFilePath, FileMode.Create);
            await request.File.CopyToAsync(stream);
        }

        // ── 5. Persist submission with Processing status ──────────────────────
        var submission = new Submission
        {
            AssignmentId = assignmentId,
            StudentId = studentId,
            AnswerText = request.AnswerText,
            SubmittedAt = DateTime.UtcNow,
            Status = SubmissionStatus.Processing
        };

        _db.Submissions.Add(submission);
        await _db.SaveChangesAsync();

        // ── 6. Enqueue background job (non-blocking) ──────────────────────────
        await _queue.EnqueueAsync(new SubmissionJob(
            submission.Id,
            tempFilePath,
            originalFileName,
            baseUrl));

        // ── 7. Return immediately — background worker does the rest ───────────
        return new SubmitResultDto
        {
            SubmissionId = submission.Id,
            Status = submission.Status.ToString()   // "Processing"
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
