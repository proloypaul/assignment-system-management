using AssignmentSystem.Application.Features.Subjects.DTOs;

namespace AssignmentSystem.Application.Features.Subjects.Interfaces;

public interface ISubjectService
{
    Task<(IEnumerable<SubjectDto> Items, int TotalCount)> GetAllAsync(int page, int pageSize, Guid? teacherId);
    Task<SubjectDto?> GetByIdAsync(Guid id);
    Task<SubjectDto> CreateAsync(CreateSubjectRequest request);
    Task UpdateAsync(Guid id, UpdateSubjectRequest request);
    Task DeleteAsync(Guid id);
    Task AssignTeacherAsync(Guid subjectId, AssignTeacherRequest request);
}
