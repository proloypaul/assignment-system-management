namespace AssignmentSystem.Application.Features.Assignments.DTOs;

public record CreateAssignmentRequest(
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    int MaxMarks,
    Guid SubjectId);

public record UpdateAssignmentRequest(
    string? Title,
    string? Description,
    DateTime? EndDate,
    int? MaxMarks);
