using Asp.Versioning;
using AssignmentSystem.Application.Features.Subjects.DTOs;
using AssignmentSystem.Application.Features.Subjects.Interfaces;
using AssignmentSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/subjects")]
[Authorize]
public class SubjectsController : ControllerBase
{
    private readonly ISubjectService _subjectService;
    private readonly ICurrentUserService _currentUser;

    public SubjectsController(ISubjectService subjectService, ICurrentUserService currentUser)
    {
        _subjectService = subjectService;
        _currentUser = currentUser;
    }

    /// <summary>Get all subjects. Teachers see only their assigned subjects.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        // Pass teacher filter only when the caller is a Teacher role
        Guid? teacherId = User.IsInRole("Teacher") ? _currentUser.UserId : null;

        var (items, totalCount) = await _subjectService.GetAllAsync(page, pageSize, teacherId);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return Ok(new { items, totalCount, page, pageSize, totalPages });
    }

    /// <summary>Get a subject by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var subject = await _subjectService.GetByIdAsync(id);
        return subject is null ? NotFound() : Ok(subject);
    }

    /// <summary>Create a new subject (Admin only).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateSubjectRequest request)
    {
        var dto = await _subjectService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>Update a subject (Admin only).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSubjectRequest request)
    {
        try
        {
            await _subjectService.UpdateAsync(id, request);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Delete a subject (Admin only).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _subjectService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Assign a teacher to a subject (Admin only).</summary>
    [HttpPost("{id:guid}/assign-teacher")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignTeacher(Guid id, [FromBody] AssignTeacherRequest request)
    {
        try
        {
            await _subjectService.AssignTeacherAsync(id, request);
            return Ok(new { message = "Teacher assigned successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
