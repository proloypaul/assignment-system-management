namespace AssignmentSystem.Application.Features.Courses.DTOs;

public record CreateCourseRequest(
    string Name,
    string Code,
    string Description,
    int Capacity,
    DateTime StartDate,
    DateTime EndDate);

public record UpdateCourseRequest(
    string? Name,
    string? Description,
    int? Capacity,
    bool? IsActive,
    DateTime? StartDate,
    DateTime? EndDate);

public record EnrollStudentRequest(Guid StudentId);
