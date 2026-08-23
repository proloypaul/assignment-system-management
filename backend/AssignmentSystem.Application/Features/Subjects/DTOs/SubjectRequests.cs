namespace AssignmentSystem.Application.Features.Subjects.DTOs;

public record CreateSubjectRequest(
    string Name,
    string Code,
    int Credits,
    string? SyllabusUrl,
    Guid CourseId);

public record UpdateSubjectRequest(
    string? Name,
    int? Credits,
    string? SyllabusUrl);

public record AssignTeacherRequest(Guid TeacherId);
