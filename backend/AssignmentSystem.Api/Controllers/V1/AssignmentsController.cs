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

    /// <summary>Get all published assignments with pagination (all roles).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = _db.Assignments
            .Include(a => a.Subject)
            .Include(a => a.Teacher)
            .Where(a => a.Status == AssignmentStatus.Published);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id, a.Title, a.Description, a.StartDate, a.EndDate, a.MaxMarks, a.Status,
                Subject = a.Subject == null ? null : new { a.Subject.Id, a.Subject.Name },
                Teacher = a.Teacher == null ? null : new { a.Teacher.Id, a.Teacher.Name }
            })
            .ToListAsync();

        return Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    /// <summary>Get a single assignment by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var assignment = await _db.Assignments
            .Include(a => a.Subject)
            .Include(a => a.Teacher)
            .FirstOrDefaultAsync(a => a.Id == id);
        return assignment is null ? NotFound() : Ok(assignment);
    }

    /// <summary>Create a new assignment (Teacher only).</summary>
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
        return CreatedAtAction(nameof(GetById), new { id = assignment.Id }, assignment);
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

    /// <summary>Delete an assignment (Admin only).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var assignment = await _db.Assignments.FindAsync(id);
        if (assignment is null) return NotFound();

        _db.Assignments.Remove(assignment);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Get all submissions for an assignment (Teacher/Admin only).</summary>
    [HttpGet("{id:guid}/submissions")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> GetSubmissions(Guid id)
    {
        var submissions = await _db.Submissions
            .Include(s => s.Student)
            .Where(s => s.AssignmentId == id)
            .ToListAsync();
        return Ok(submissions);
    }
}

public record CreateAssignmentRequest(string Title, string Description, DateTime StartDate, DateTime EndDate, int MaxMarks, Guid SubjectId);
public record UpdateAssignmentRequest(string? Title, string? Description, DateTime? EndDate, int? MaxMarks);
