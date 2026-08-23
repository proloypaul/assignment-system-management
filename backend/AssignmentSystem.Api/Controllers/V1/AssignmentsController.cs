using Asp.Versioning;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Features.Assignments.DTOs;
using AssignmentSystem.Application.Features.Assignments.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/assignments")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;
    private readonly ICurrentUserService _currentUser;

    public AssignmentsController(IAssignmentService assignmentService, ICurrentUserService currentUser)
    {
        _assignmentService = assignmentService;
        _currentUser = currentUser;
    }

    /// <summary>Get all published assignments with pagination — Students see only their enrolled courses' assignments.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var isStudent = User.IsInRole("Student");
        var isTeacher = User.IsInRole("Teacher");

        var (items, totalCount) = await _assignmentService.GetAllAsync(page, pageSize, isStudent, isTeacher, _currentUser.UserId);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    /// <summary>Get a single assignment by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var assignment = await _assignmentService.GetByIdAsync(id, _currentUser.UserId);
        return assignment is null ? NotFound() : Ok(assignment);
    }

    /// <summary>Create a new assignment (Teacher/Admin only). Saved as Draft.</summary>
    [HttpPost]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateAssignmentRequest request)
    {
        var teacherId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var dto = await _assignmentService.CreateAsync(request, teacherId);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, new { dto.Id, dto.Title, dto.Status });
    }

    /// <summary>Publish an assignment (Teacher/Admin only).</summary>
    [HttpPut("{id:guid}/publish")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Publish(Guid id)
    {
        try
        {
            await _assignmentService.PublishAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Update an assignment (Teacher/Admin only).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAssignmentRequest request)
    {
        try
        {
            await _assignmentService.UpdateAsync(id, request);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Delete an assignment (Admin or Teacher).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var isTeacher = User.IsInRole("Teacher");
            await _assignmentService.DeleteAsync(id, isTeacher, _currentUser.UserId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>Get all submissions for an assignment (Teacher/Admin only).</summary>
    [HttpGet("{id:guid}/submissions")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> GetSubmissions(Guid id)
    {
        var submissions = await _assignmentService.GetSubmissionsAsync(id);
        return Ok(submissions);
    }
}
