using Asp.Versioning;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/subjects")]
[Authorize]
public class SubjectsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SubjectsController(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Get all subjects projected to DTO to avoid circular refs.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = _db.Subjects
            .Include(s => s.Course)
            .Include(s => s.Teachers)
                .ThenInclude(ta => ta.Teacher)
            .AsQueryable();

        if (User.IsInRole("Teacher"))
        {
            var teacherId = _currentUser.UserId;
            if (teacherId.HasValue)
            {
                query = query.Where(s => s.Teachers.Any(t => t.TeacherId == teacherId.Value));
            }
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var subjects = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Credits = s.Credits,
                SyllabusUrl = s.SyllabusUrl,
                CourseId = s.CourseId,
                CourseName = s.Course != null ? s.Course.Name : null,
                CourseCode = s.Course != null ? s.Course.Code : null,
                TeacherNames = s.Teachers.Select(ta => ta.Teacher.Name!).ToList()
            })
            .ToListAsync();

        return Ok(new { items = subjects, totalCount, page, pageSize, totalPages });
    }

    /// <summary>Get a subject by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var subject = await _db.Subjects
            .Include(s => s.Course)
            .Include(s => s.Teachers)
                .ThenInclude(ta => ta.Teacher)
            .Where(s => s.Id == id)
            .Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Credits = s.Credits,
                SyllabusUrl = s.SyllabusUrl,
                CourseId = s.CourseId,
                CourseName = s.Course != null ? s.Course.Name : null,
                CourseCode = s.Course != null ? s.Course.Code : null,
                TeacherNames = s.Teachers.Select(ta => ta.Teacher.Name!).ToList()
            })
            .FirstOrDefaultAsync();

        return subject is null ? NotFound() : Ok(subject);
    }

    /// <summary>Create a new subject (Admin only).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateSubjectRequest request)
    {
        var subject = new Subject
        {
            Name = request.Name,
            Code = request.Code,
            Credits = request.Credits,
            SyllabusUrl = request.SyllabusUrl,
            CourseId = request.CourseId
        };
        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync();

        var dto = new SubjectDto
        {
            Id = subject.Id,
            Name = subject.Name,
            Code = subject.Code,
            Credits = subject.Credits,
            SyllabusUrl = subject.SyllabusUrl,
            CourseId = subject.CourseId
        };

        return CreatedAtAction(nameof(GetById), new { id = subject.Id }, dto);
    }

    /// <summary>Update a subject (Admin only).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSubjectRequest request)
    {
        var subject = await _db.Subjects.FindAsync(id);
        if (subject is null) return NotFound();

        subject.Name = request.Name ?? subject.Name;
        subject.Credits = request.Credits ?? subject.Credits;
        subject.SyllabusUrl = request.SyllabusUrl ?? subject.SyllabusUrl;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Delete a subject (Admin only).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var subject = await _db.Subjects.FindAsync(id);
        if (subject is null) return NotFound();

        _db.Subjects.Remove(subject);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Assign a teacher to a subject (Admin only).</summary>
    [HttpPost("{id:guid}/assign-teacher")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignTeacher(Guid id, [FromBody] AssignTeacherRequest request)
    {
        var subject = await _db.Subjects.FindAsync(id);
        if (subject is null) return NotFound("Subject not found.");

        var teacher = await _db.Users.FindAsync(request.TeacherId);
        if (teacher is null) return NotFound("Teacher not found.");

        var existingAssignments = await _db.Set<TeacherSubjectAssignment>()
            .Where(ts => ts.SubjectId == id)
            .ToListAsync();

        if (existingAssignments.Any())
        {
            _db.Set<TeacherSubjectAssignment>().RemoveRange(existingAssignments);
        }

        _db.Set<TeacherSubjectAssignment>().Add(new TeacherSubjectAssignment
        {
            SubjectId = id,
            TeacherId = request.TeacherId,
            AssignedDate = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return Ok(new { message = "Teacher assigned successfully." });
    }
}

// ─── DTOs ──────────────────────────────────────────────────────────────────────
public class SubjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int Credits { get; set; }
    public string? SyllabusUrl { get; set; }
    public Guid CourseId { get; set; }
    public string? CourseName { get; set; }
    public string? CourseCode { get; set; }
    public List<string> TeacherNames { get; set; } = new();
}

// ─── Request records ──────────────────────────────────────────────────────────
public record CreateSubjectRequest(string Name, string Code, int Credits, string? SyllabusUrl, Guid CourseId);
public record UpdateSubjectRequest(string? Name, int? Credits, string? SyllabusUrl);
public record AssignTeacherRequest(Guid TeacherId);
