namespace AssignmentSystem.Application.Features.Subjects.DTOs;

public class SubjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int Credits { get; set; }
    public string? SyllabusUrl { get; set; }
    public Guid CourseId { get; set; }
    public string? CourseName { get; set; }
    public string? CourseCode { get; set; }
    public List<string> TeacherNames { get; set; } = new();
}
