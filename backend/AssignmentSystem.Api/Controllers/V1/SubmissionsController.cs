using Asp.Versioning;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/submissions")]
[Authorize]
public class SubmissionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SubmissionsController(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Submit an answer for an assignment (Student only).
    /// Returns 202 Accepted immediately; background processing updates the status.
    /// </summary>
    [HttpPost("{assignmentId:guid}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Submit(Guid assignmentId, [FromBody] SubmitRequest request)
    {
        var studentId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var assignment = await _db.Assignments.FindAsync(assignmentId);
        if (assignment is null) return NotFound();
        if (assignment.Status != AssignmentStatus.Published)
            return BadRequest(new { message = "Assignment is not published." });
        if (DateTime.UtcNow > assignment.EndDate)
            return BadRequest(new { message = "Submission deadline has passed." });

        var existing = await _db.Submissions
            .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);
        if (existing is not null)
            return Conflict(new { message = "You have already submitted for this assignment." });

        var submission = new Submission
        {
            AssignmentId = assignmentId,
            StudentId = studentId,
            AnswerText = request.AnswerText,
            SubmittedAt = DateTime.UtcNow,
            Status = SubmissionStatus.Processing // Background worker will set to Submitted
        };

        _db.Submissions.Add(submission);
        await _db.SaveChangesAsync();

        // TODO: Push to async background queue (Hangfire/RabbitMQ) in a later phase
        // For now, immediately mark as Submitted
        submission.Status = SubmissionStatus.Submitted;
        await _db.SaveChangesAsync();

        return Accepted(new { submissionId = submission.Id, status = submission.Status.ToString() });
    }

    /// <summary>Get a student's own submission for an assignment.</summary>
    [HttpGet("{assignmentId:guid}/my")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMySubmission(Guid assignmentId)
    {
        var studentId = _currentUser.UserId;
        var submission = await _db.Submissions
            .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);
        return submission is null ? NotFound() : Ok(submission);
    }

    /// <summary>Grade a submission (Teacher/Admin only).</summary>
    [HttpPost("{submissionId:guid}/grade")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Grade(Guid submissionId, [FromBody] GradeRequest request)
    {
        var submission = await _db.Submissions.FindAsync(submissionId);
        if (submission is null) return NotFound();

        submission.MarksAwarded = request.MarksAwarded;
        submission.Feedback = request.Feedback;
        submission.Status = SubmissionStatus.Graded;

        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record SubmitRequest(string? AnswerText);
public record GradeRequest(int MarksAwarded, string? Feedback);
