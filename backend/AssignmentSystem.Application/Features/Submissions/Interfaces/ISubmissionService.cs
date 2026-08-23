using AssignmentSystem.Application.Features.Submissions.DTOs;

namespace AssignmentSystem.Application.Features.Submissions.Interfaces;

public interface ISubmissionService
{
    Task<SubmitResultDto> SubmitAsync(Guid assignmentId, Guid studentId, SubmitRequest request, string baseUrl);
    Task<IEnumerable<MySubmissionDto>> GetMySubmissionsAsync(Guid studentId);
    Task<MySubmissionDto?> GetMySubmissionAsync(Guid assignmentId, Guid studentId);
    Task GradeAsync(Guid submissionId, GradeRequest request);
}
