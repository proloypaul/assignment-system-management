using AssignmentSystem.Application.Features.Courses.DTOs;

namespace AssignmentSystem.Application.Features.Courses.Interfaces;

public interface ICourseService
{
    Task<(IEnumerable<CourseDto> Items, int TotalCount)> GetAllAsync(int page, int pageSize, Guid? currentUserId);
    Task<CourseDto?> GetByIdAsync(Guid id);
    Task<CourseDto> CreateAsync(CreateCourseRequest request);
    Task UpdateAsync(Guid id, UpdateCourseRequest request);
    Task DeleteAsync(Guid id);

    /// <summary>Admin assigns a student to a course.</summary>
    Task EnrollStudentAsync(Guid courseId, EnrollStudentRequest request);

    /// <summary>Student self-enroll with business rules (date + active enrollment check).</summary>
    Task StudentEnrollAsync(Guid courseId, Guid studentId);

    Task<IEnumerable<object>> GetEnrollmentsAsync(Guid courseId);
    Task RemoveEnrollmentAsync(Guid courseId, Guid studentId);
}
