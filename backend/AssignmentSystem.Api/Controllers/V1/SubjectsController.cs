using Asp.Versioning;
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

    public SubjectsController(ApplicationDbContext db) => _db = db;

    /// <summary>Get all subjects.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var subjects = await _db.Subjects.Include(s => s.Course).ToListAsync();
        return Ok(subjects);
    }

    /// <summary>Get a subject by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var subject = await _db.Subjects.Include(s => s.Course).FirstOrDefaultAsync(s => s.Id == id);
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
        return CreatedAtAction(nameof(GetById), new { id = subject.Id }, subject);
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
}

public record CreateSubjectRequest(string Name, string Code, int Credits, string? SyllabusUrl, Guid CourseId);
public record UpdateSubjectRequest(string? Name, int? Credits, string? SyllabusUrl);
