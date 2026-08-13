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
[Route("api/v{version:apiVersion}/courses")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CoursesController(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Get all courses with their subjects projected to avoid circular refs.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var studentId = _currentUser.UserId;
        var query = _db.Courses.AsQueryable();

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var courses = await query
            .Include(c => c.Subjects)
            .Include(c => c.Enrollments)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CourseDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Description = c.Description,
                Capacity = c.Capacity,
                IsActive = c.IsActive,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                IsEnrolled = studentId.HasValue ? c.Enrollments.Any(e => e.StudentId == studentId.Value) : false,
                Subjects = c.Subjects.Select(s => new SubjectSummaryDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Code = s.Code,
                    Credits = s.Credits
                }).ToList()
            })
            .ToListAsync();

        return Ok(new { items = courses, totalCount, page, pageSize, totalPages });
    }

    /// <summary>Get a course by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var course = await _db.Courses
            .Include(c => c.Subjects)
            .Where(c => c.Id == id)
            .Select(c => new CourseDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Description = c.Description,
                Capacity = c.Capacity,
                IsActive = c.IsActive,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Subjects = c.Subjects.Select(s => new SubjectSummaryDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Code = s.Code,
                    Credits = s.Credits
                }).ToList()
            })
            .FirstOrDefaultAsync();

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

        var dto = new CourseDto
        {
            Id = course.Id,
            Name = course.Name,
            Code = course.Code,
            Description = course.Description,
            Capacity = course.Capacity,
            IsActive = course.IsActive,
            StartDate = course.StartDate,
            EndDate = course.EndDate,
            Subjects = []
        };

        return CreatedAtAction(nameof(GetById), new { id = course.Id }, dto);
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
        course.StartDate = request.StartDate ?? course.StartDate;
        course.EndDate = request.EndDate ?? course.EndDate;

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

    /// <summary>Student self-enrollment with rules (Student only).</summary>
    [HttpPost("{id:guid}/enroll")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> StudentEnroll(Guid id)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course is null) return NotFound("Course not found.");

        var now = DateTime.UtcNow;
        if (now < course.StartDate || now > course.EndDate)
            return BadRequest(new { message = "Course is not currently open for enrollment." });

        var studentId = _currentUser.UserId;
        if (studentId == null) return Unauthorized();

        var existingEnrollment = await _db.Set<CourseEnrollment>()
            .Include(ce => ce.Course)
            .Where(ce => ce.StudentId == studentId)
            .ToListAsync();

        if (existingEnrollment.Any(ce => ce.CourseId == id))
            return Conflict(new { message = "You are already enrolled in this course." });

        if (existingEnrollment.Any(ce => ce.Course.EndDate >= now))
            return BadRequest(new { message = "You are currently enrolled in another active course. You can only enroll in a new course after your current course ends." });

        var enrollment = new CourseEnrollment
        {
            CourseId = id,
            StudentId = studentId.Value,
            EnrolledAt = DateTime.UtcNow
        };

        _db.Set<CourseEnrollment>().Add(enrollment);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Enrolled successfully." });
    }

    /// <summary>Get all enrolled students for a course (Admin only).</summary>
    [HttpGet("{id:guid}/enrollments")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetEnrollments(Guid id)
    {
        var enrollments = await _db.Set<CourseEnrollment>()
            .Include(ce => ce.Student)
            .Where(ce => ce.CourseId == id)
            .Select(ce => new
            {
                ce.StudentId,
                StudentName = ce.Student.Name,
                StudentEmail = ce.Student.Email,
                ce.EnrolledAt
            })
            .ToListAsync();

        return Ok(enrollments);
    }

    /// <summary>Remove a student enrollment from a course (Admin only).</summary>
    [HttpDelete("{id:guid}/enrollments/{studentId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveEnrollment(Guid id, Guid studentId)
    {
        var enrollment = await _db.Set<CourseEnrollment>()
            .FirstOrDefaultAsync(ce => ce.CourseId == id && ce.StudentId == studentId);
            
        if (enrollment == null) return NotFound("Enrollment not found.");

        _db.Set<CourseEnrollment>().Remove(enrollment);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

// ─── DTOs ──────────────────────────────────────────────────────────────────────
public class CourseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Capacity { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsEnrolled { get; set; }
    public List<SubjectSummaryDto> Subjects { get; set; } = [];
}

public class SubjectSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int Credits { get; set; }
}

// ─── Request records ──────────────────────────────────────────────────────────
public record CreateCourseRequest(string Name, string Code, string Description, int Capacity, DateTime StartDate, DateTime EndDate);
public record UpdateCourseRequest(string? Name, string? Description, int? Capacity, bool? IsActive, DateTime? StartDate, DateTime? EndDate);
public record EnrollStudentRequest(Guid StudentId);
