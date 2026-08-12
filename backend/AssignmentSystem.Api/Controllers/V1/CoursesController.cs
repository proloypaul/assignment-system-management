using Asp.Versioning;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/courses")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CoursesController(ApplicationDbContext db) => _db = db;

    /// <summary>Get all courses.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var courses = await _db.Courses
            .Include(c => c.Subjects)
            .ToListAsync();
        return Ok(courses);
    }

    /// <summary>Get a course by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var course = await _db.Courses.Include(c => c.Subjects).FirstOrDefaultAsync(c => c.Id == id);
        return course is null ? NotFound() : Ok(course);
    }

    /// <summary>Create a new course (Admin only).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCourseRequest request)
    {
        var course = new Course
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            Capacity = request.Capacity,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = true
        };
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = course.Id }, course);
    }

    /// <summary>Update a course (Admin only).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCourseRequest request)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course is null) return NotFound();

        course.Name = request.Name ?? course.Name;
        course.Description = request.Description ?? course.Description;
        course.Capacity = request.Capacity ?? course.Capacity;
        course.IsActive = request.IsActive ?? course.IsActive;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Delete a course (Admin only).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course is null) return NotFound();

        _db.Courses.Remove(course);
        await _db.SaveChangesAsync();
        return NoContent();
    }
    /// <summary>Enroll a student in a course (Admin only).</summary>
    [HttpPost("{id:guid}/enroll-student")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EnrollStudent(Guid id, [FromBody] EnrollStudentRequest request)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course is null) return NotFound("Course not found.");

        var student = await _db.Users.FindAsync(request.StudentId);
        if (student is null) return NotFound("Student not found.");

        var existing = await _db.Set<CourseEnrollment>()
            .FirstOrDefaultAsync(ce => ce.CourseId == id && ce.StudentId == request.StudentId);
            
        if (existing is not null)
            return Conflict(new { message = "Student is already enrolled in this course." });

        var enrollment = new CourseEnrollment
        {
            CourseId = id,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        };

        _db.Set<CourseEnrollment>().Add(enrollment);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Student enrolled successfully." });
    }
}

public record CreateCourseRequest(string Name, string Code, string Description, int Capacity, DateTime StartDate, DateTime EndDate);
public record UpdateCourseRequest(string? Name, string? Description, int? Capacity, bool? IsActive);
public record EnrollStudentRequest(Guid StudentId);
