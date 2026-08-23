using Asp.Versioning;
using AssignmentSystem.Application.Features.Courses.DTOs;
using AssignmentSystem.Application.Features.Courses.Interfaces;
using AssignmentSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/courses")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly ICurrentUserService _currentUser;

    public CoursesController(ICourseService courseService, ICurrentUserService currentUser)
    {
        _courseService = courseService;
        _currentUser = currentUser;
    }

    /// <summary>Get all courses with pagination.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (items, totalCount) = await _courseService.GetAllAsync(page, pageSize, _currentUser.UserId);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    /// <summary>Get a course by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var course = await _courseService.GetByIdAsync(id);
        return course is null ? NotFound() : Ok(course);
    }

    /// <summary>Create a new course (Admin only).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCourseRequest request)
    {
        var dto = await _courseService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>Update a course (Admin only).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCourseRequest request)
    {
        try
        {
            await _courseService.UpdateAsync(id, request);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Delete a course (Admin only).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _courseService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Admin enrolls a student in a course.</summary>
    [HttpPost("{id:guid}/enroll-student")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EnrollStudent(Guid id, [FromBody] EnrollStudentRequest request)
    {
        try
        {
            await _courseService.EnrollStudentAsync(id, request);
            return Ok(new { message = "Student enrolled successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Student self-enrollment with business rules.</summary>
    [HttpPost("{id:guid}/enroll")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> StudentEnroll(Guid id)
    {
        var studentId = _currentUser.UserId;
        if (studentId is null) return Unauthorized();

        try
        {
            await _courseService.StudentEnrollAsync(id, studentId.Value);
            return Ok(new { message = "Enrolled successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Distinguish conflict vs bad request by message content
            return ex.Message.Contains("already enrolled")
                ? Conflict(new { message = ex.Message })
                : BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Get all enrolled students for a course (Admin only).</summary>
    [HttpGet("{id:guid}/enrollments")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetEnrollments(Guid id)
    {
        var enrollments = await _courseService.GetEnrollmentsAsync(id);
        return Ok(enrollments);
    }

    /// <summary>Remove a student enrollment from a course (Admin only).</summary>
    [HttpDelete("{id:guid}/enrollments/{studentId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveEnrollment(Guid id, Guid studentId)
    {
        try
        {
            await _courseService.RemoveEnrollmentAsync(id, studentId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
