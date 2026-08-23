using AssignmentSystem.Application.Features.Assignments.DTOs;

namespace AssignmentSystem.Application.Features.Assignments.Interfaces;

public interface IAssignmentService
{
    Task<(IEnumerable<AssignmentDto> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, bool isStudent, bool isTeacher, Guid? currentUserId);
    Task<AssignmentDto?> GetByIdAsync(Guid id, Guid? currentUserId);
    Task<AssignmentDto> CreateAsync(CreateAssignmentRequest request, Guid teacherId);
    Task PublishAsync(Guid id);
    Task UpdateAsync(Guid id, UpdateAssignmentRequest request);
    Task DeleteAsync(Guid id, bool isTeacher, Guid? currentUserId);
    Task<IEnumerable<SubmissionDto>> GetSubmissionsAsync(Guid assignmentId);
}
