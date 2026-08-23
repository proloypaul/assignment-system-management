using AssignmentSystem.Application.Features.Subjects.DTOs;
using AssignmentSystem.Application.Features.Subjects.Interfaces;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Infrastructure.Services;

public class SubjectService : ISubjectService
{
    private readonly ApplicationDbContext _db;

    public SubjectService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(IEnumerable<SubjectDto> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, Guid? teacherId)
    {
        var query = _db.Subjects
            .Include(s => s.Course)
            .Include(s => s.Teachers)
                .ThenInclude(ta => ta.Teacher)
            .AsQueryable();

        // When a teacher is requesting, filter to their subjects only
        if (teacherId.HasValue)
        {
            query = query.Where(s => s.Teachers.Any(t => t.TeacherId == teacherId.Value));
        }

        var totalCount = await query.CountAsync();

        var subjects = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Credits = s.Credits,
                SyllabusUrl = s.SyllabusUrl,
                CourseId = s.CourseId,
                CourseName = s.Course != null ? s.Course.Name : null,
                CourseCode = s.Course != null ? s.Course.Code : null,
                TeacherNames = s.Teachers.Select(ta => ta.Teacher.Name!).ToList()
            })
            .ToListAsync();

        return (subjects, totalCount);
    }

    public async Task<SubjectDto?> GetByIdAsync(Guid id)
    {
        return await _db.Subjects
            .Include(s => s.Course)
            .Include(s => s.Teachers)
                .ThenInclude(ta => ta.Teacher)
            .Where(s => s.Id == id)
            .Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Credits = s.Credits,
                SyllabusUrl = s.SyllabusUrl,
                CourseId = s.CourseId,
                CourseName = s.Course != null ? s.Course.Name : null,
                CourseCode = s.Course != null ? s.Course.Code : null,
                TeacherNames = s.Teachers.Select(ta => ta.Teacher.Name!).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<SubjectDto> CreateAsync(CreateSubjectRequest request)
    {
        var subject = new Subject
        {
            Name = request.Name,
            Code = request.Code,
            Credits = request.Credits,
            SyllabusUrl = request.SyllabusUrl,
            CourseId = request.CourseId
        };

        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync();

        return new SubjectDto
        {
            Id = subject.Id,
            Name = subject.Name,
            Code = subject.Code,
            Credits = subject.Credits,
            SyllabusUrl = subject.SyllabusUrl,
            CourseId = subject.CourseId
        };
    }

    public async Task UpdateAsync(Guid id, UpdateSubjectRequest request)
    {
        var subject = await _db.Subjects.FindAsync(id)
            ?? throw new KeyNotFoundException($"Subject '{id}' not found.");

        subject.Name = request.Name ?? subject.Name;
        subject.Credits = request.Credits ?? subject.Credits;
        subject.SyllabusUrl = request.SyllabusUrl ?? subject.SyllabusUrl;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var subject = await _db.Subjects.FindAsync(id)
            ?? throw new KeyNotFoundException($"Subject '{id}' not found.");

        _db.Subjects.Remove(subject);
        await _db.SaveChangesAsync();
    }

    public async Task AssignTeacherAsync(Guid subjectId, AssignTeacherRequest request)
    {
        var subject = await _db.Subjects.FindAsync(subjectId)
            ?? throw new KeyNotFoundException("Subject not found.");

        var teacher = await _db.Users.FindAsync(request.TeacherId)
            ?? throw new KeyNotFoundException("Teacher not found.");

        // Replace existing assignment (one teacher per subject policy)
        var existingAssignments = await _db.Set<TeacherSubjectAssignment>()
            .Where(ts => ts.SubjectId == subjectId)
            .ToListAsync();

        if (existingAssignments.Any())
        {
            _db.Set<TeacherSubjectAssignment>().RemoveRange(existingAssignments);
        }

        _db.Set<TeacherSubjectAssignment>().Add(new TeacherSubjectAssignment
        {
            SubjectId = subjectId,
            TeacherId = request.TeacherId,
            AssignedDate = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}
