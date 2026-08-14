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
[Route("api/v{version:apiVersion}/assignments")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AssignmentsController(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Get all published assignments with pagination — Students see only their enrolled courses' assignments.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = _db.Assignments.AsQueryable();

        if (User.IsInRole("Student"))
        {
            query = query.Where(a => a.Status == AssignmentStatus.Published);
            var studentId = _currentUser.UserId;
            if (studentId.HasValue)
            {
                var enrolledCourseIds = _db.Set<CourseEnrollment>()
                    .Where(ce => ce.StudentId == studentId.Value)
                    .Select(ce => ce.CourseId);

                query = query.Where(a => a.Subject != null && enrolledCourseIds.Contains(a.Subject.CourseId));
            }
        }
        else if (User.IsInRole("Teacher"))
        {
            var teacherId = _currentUser.UserId;
            if (teacherId.HasValue)
            {
                query = query.Where(a => a.TeacherId == teacherId.Value);
            }
        }
        // Admins see all assignments, so no filter is needed for them.

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AssignmentDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                MaxMarks = a.MaxMarks,
                Status = a.Status.ToString(),
                Subject = a.Subject == null ? null : new AssignmentSubjectDto { Id = a.Subject.Id, Name = a.Subject.Name },
                Teacher = a.Teacher == null ? null : new AssignmentTeacherDto { Id = a.Teacher.Id, Name = a.Teacher.Name },
                IsSubmitted = _currentUser.UserId.HasValue ? a.Submissions.Any(s => s.StudentId == _currentUser.UserId.Value) : false
            })
            .ToListAsync();

        return Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    /// <summary>Get a single assignment by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var assignment = await _db.Assignments
            .Where(a => a.Id == id)
            .Select(a => new AssignmentDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                MaxMarks = a.MaxMarks,
                Status = a.Status.ToString(),
                Subject = a.Subject == null ? null : new AssignmentSubjectDto { Id = a.Subject.Id, Name = a.Subject.Name },
                Teacher = a.Teacher == null ? null : new AssignmentTeacherDto { Id = a.Teacher.Id, Name = a.Teacher.Name },
                IsSubmitted = _currentUser.UserId.HasValue ? a.Submissions.Any(s => s.StudentId == _currentUser.UserId.Value) : false
            })
            .FirstOrDefaultAsync();

        return assignment is null ? NotFound() : Ok(assignment);
    }

    /// <summary>Create a new assignment (Teacher/Admin only). Saved as Draft.</summary>
    [HttpPost]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateAssignmentRequest request)
    {
        var teacherId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var assignment = new Assignment
        {
            Title = request.Title,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            MaxMarks = request.MaxMarks,
            SubjectId = request.SubjectId,
            TeacherId = teacherId,
            Status = AssignmentStatus.Draft
        };
        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = assignment.Id }, new { assignment.Id, assignment.Title, assignment.Status });
    }

    /// <summary>Publish an assignment (Teacher/Admin only).</summary>
    [HttpPut("{id:guid}/publish")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Publish(Guid id)
    {
        var assignment = await _db.Assignments.FindAsync(id);
        if (assignment is null) return NotFound();

        assignment.Status = AssignmentStatus.Published;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Update an assignment (Teacher/Admin only).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAssignmentRequest request)
    {
        var assignment = await _db.Assignments.FindAsync(id);
        if (assignment is null) return NotFound();

        assignment.Title = request.Title ?? assignment.Title;
        assignment.Description = request.Description ?? assignment.Description;
        assignment.EndDate = request.EndDate ?? assignment.EndDate;
        assignment.MaxMarks = request.MaxMarks ?? assignment.MaxMarks;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Delete an assignment (Admin or Teacher).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var assignment = await _db.Assignments.FindAsync(id);
        if (assignment is null) return NotFound();

        if (User.IsInRole("Teacher") && assignment.TeacherId != _currentUser.UserId)
            return Forbid();

        _db.Assignments.Remove(assignment);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Get all submissions for an assignment (Teacher/Admin only) — projected to avoid cycles.</summary>
    [HttpGet("{id:guid}/submissions")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> GetSubmissions(Guid id)
    {
        var submissions = await _db.Submissions
            .Include(s => s.Assignment)
                .ThenInclude(a => a.Teacher)
            .Include(s => s.Assignment)
                .ThenInclude(a => a.Subject)
            .Where(s => s.AssignmentId == id)
            .Select(s => new SubmissionDto
            {
                Id = s.Id,
                AssignmentId = s.AssignmentId,
                AssignmentTitle = s.Assignment.Title,
                TeacherName = s.Assignment.Teacher != null ? s.Assignment.Teacher.Name : null,
                SubjectName = s.Assignment.Subject != null ? s.Assignment.Subject.Name : null,
                StudentId = s.StudentId,
                StudentName = s.Student != null ? s.Student.Name : null,
                StudentEmail = s.Student != null ? s.Student.Email : null,
                AnswerText = s.AnswerText,
                AttachmentFileUrl = s.AttachmentFileUrl,
                Status = s.Status.ToString(),
                MarksAwarded = s.MarksAwarded,
                Feedback = s.Feedback,
                SubmittedAt = s.SubmittedAt
            })
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        return Ok(submissions);
    }
}

// ─── DTOs ──────────────────────────────────────────────────────────────────────
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

// ─── Request records ──────────────────────────────────────────────────────────
public record CreateAssignmentRequest(string Title, string Description, DateTime StartDate, DateTime EndDate, int MaxMarks, Guid SubjectId);
public record UpdateAssignmentRequest(string? Title, string? Description, DateTime? EndDate, int? MaxMarks);
