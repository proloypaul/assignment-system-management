using Asp.Versioning;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Features.Submissions.DTOs;
using AssignmentSystem.Application.Features.Submissions.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/submissions")]
[Authorize]
public class SubmissionsController : ControllerBase
{
    private readonly ISubmissionService _submissionService;
    private readonly ICurrentUserService _currentUser;

    public SubmissionsController(ISubmissionService submissionService, ICurrentUserService currentUser)
    {
        _submissionService = submissionService;
        _currentUser = currentUser;
    }

    /// <summary>Submit an assignment answer with optional PDF attachment (Student only).</summary>
    [HttpPost("{assignmentId:guid}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Submit(Guid assignmentId, [FromForm] SubmitRequest request)
    {
        var studentId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        try
        {
            var result = await _submissionService.SubmitAsync(assignmentId, studentId, request, baseUrl);
            return Accepted(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message.Contains("already submitted")
                ? Conflict(new { message = ex.Message })
                : BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Get all submissions made by the current student — projected to DTO.</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMySubmissions()
    {
        var studentId = _currentUser.UserId;
        if (!studentId.HasValue) return Unauthorized();

        var submissions = await _submissionService.GetMySubmissionsAsync(studentId.Value);
        return Ok(submissions);
    }

    /// <summary>Get a student's own submission for a specific assignment.</summary>
    [HttpGet("{assignmentId:guid}/my")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMySubmission(Guid assignmentId)
    {
        var studentId = _currentUser.UserId;
        if (!studentId.HasValue) return Unauthorized();

        var submission = await _submissionService.GetMySubmissionAsync(assignmentId, studentId.Value);
        return submission is null ? NotFound() : Ok(submission);
    }

    /// <summary>Grade a submission (Teacher/Admin only).</summary>
    [HttpPost("{submissionId:guid}/grade")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<IActionResult> Grade(Guid submissionId, [FromBody] GradeRequest request)
    {
        try
        {
            await _submissionService.GradeAsync(submissionId, request);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
