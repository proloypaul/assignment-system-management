using AssignmentSystem.Application.Features.Assignments.DTOs;
using AssignmentSystem.Application.Features.Assignments.Interfaces;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Infrastructure.Services;

public class AssignmentService : IAssignmentService
{
    private readonly ApplicationDbContext _db;

    public AssignmentService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(IEnumerable<AssignmentDto> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, bool isStudent, bool isTeacher, Guid? currentUserId)
    {
        var query = _db.Assignments.AsQueryable();

        if (isStudent)
        {
            query = query.Where(a => a.Status == AssignmentStatus.Published);
            if (currentUserId.HasValue)
            {
                var enrolledCourseIds = _db.Set<CourseEnrollment>()
                    .Where(ce => ce.StudentId == currentUserId.Value)
                    .Select(ce => ce.CourseId);

                query = query.Where(a => a.Subject != null && enrolledCourseIds.Contains(a.Subject.CourseId));
            }
        }
        else if (isTeacher)
        {
            if (currentUserId.HasValue)
            {
                query = query.Where(a => a.TeacherId == currentUserId.Value);
            }
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AssignmentDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                MaxMarks = a.MaxMarks,
                Status = a.Status.ToString(),
                Subject = a.Subject == null ? null : new AssignmentSubjectDto { Id = a.Subject.Id, Name = a.Subject.Name },
                Teacher = a.Teacher == null ? null : new AssignmentTeacherDto { Id = a.Teacher.Id, Name = a.Teacher.Name },
                IsSubmitted = currentUserId.HasValue ? a.Submissions.Any(s => s.StudentId == currentUserId.Value) : false
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<AssignmentDto?> GetByIdAsync(Guid id, Guid? currentUserId)
    {
        return await _db.Assignments
            .Where(a => a.Id == id)
            .Select(a => new AssignmentDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                MaxMarks = a.MaxMarks,
                Status = a.Status.ToString(),
                Subject = a.Subject == null ? null : new AssignmentSubjectDto { Id = a.Subject.Id, Name = a.Subject.Name },
                Teacher = a.Teacher == null ? null : new AssignmentTeacherDto { Id = a.Teacher.Id, Name = a.Teacher.Name },
                IsSubmitted = currentUserId.HasValue ? a.Submissions.Any(s => s.StudentId == currentUserId.Value) : false
            })
            .FirstOrDefaultAsync();
    }

    public async Task<AssignmentDto> CreateAsync(CreateAssignmentRequest request, Guid teacherId)
    {
        var assignment = new Assignment
        {
            Title = request.Title,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            MaxMarks = request.MaxMarks,
            SubjectId = request.SubjectId,
            TeacherId = teacherId,
            Status = AssignmentStatus.Draft
        };

        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync();

        return new AssignmentDto
        {
            Id = assignment.Id,
            Title = assignment.Title,
            Description = assignment.Description,
            StartDate = assignment.StartDate,
            EndDate = assignment.EndDate,
            MaxMarks = assignment.MaxMarks,
            Status = assignment.Status.ToString()
        };
    }

    public async Task PublishAsync(Guid id)
    {
        var assignment = await _db.Assignments.FindAsync(id)
            ?? throw new KeyNotFoundException($"Assignment '{id}' not found.");

        assignment.Status = AssignmentStatus.Published;
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Guid id, UpdateAssignmentRequest request)
    {
        var assignment = await _db.Assignments.FindAsync(id)
            ?? throw new KeyNotFoundException($"Assignment '{id}' not found.");

        assignment.Title = request.Title ?? assignment.Title;
        assignment.Description = request.Description ?? assignment.Description;
        assignment.EndDate = request.EndDate ?? assignment.EndDate;
        assignment.MaxMarks = request.MaxMarks ?? assignment.MaxMarks;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id, bool isTeacher, Guid? currentUserId)
    {
        var assignment = await _db.Assignments.FindAsync(id)
            ?? throw new KeyNotFoundException($"Assignment '{id}' not found.");

        if (isTeacher && assignment.TeacherId != currentUserId)
            throw new UnauthorizedAccessException("Teachers can only delete their own assignments.");

        _db.Assignments.Remove(assignment);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<SubmissionDto>> GetSubmissionsAsync(Guid assignmentId)
    {
        return await _db.Submissions
            .Include(s => s.Assignment)
                .ThenInclude(a => a.Teacher)
            .Include(s => s.Assignment)
                .ThenInclude(a => a.Subject)
            .Where(s => s.AssignmentId == assignmentId)
            .Select(s => new SubmissionDto
            {
                Id = s.Id,
                AssignmentId = s.AssignmentId,
                AssignmentTitle = s.Assignment.Title,
                TeacherName = s.Assignment.Teacher != null ? s.Assignment.Teacher.Name : null,
                SubjectName = s.Assignment.Subject != null ? s.Assignment.Subject.Name : null,
                StudentId = s.StudentId,
                StudentName = s.Student != null ? s.Student.Name : null,
                StudentEmail = s.Student != null ? s.Student.Email : null,
                AnswerText = s.AnswerText,
                AttachmentFileUrl = s.AttachmentFileUrl,
                Status = s.Status.ToString(),
                MarksAwarded = s.MarksAwarded,
                Feedback = s.Feedback,
                SubmittedAt = s.SubmittedAt
            })
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();
    }
}
