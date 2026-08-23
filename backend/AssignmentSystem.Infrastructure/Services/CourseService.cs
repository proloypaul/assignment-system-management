using AssignmentSystem.Application.Features.Courses.DTOs;
using AssignmentSystem.Application.Features.Courses.Interfaces;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Infrastructure.Services;

public class CourseService : ICourseService
{
    private readonly ApplicationDbContext _db;

    public CourseService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(IEnumerable<CourseDto> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, Guid? currentUserId)
    {
        var query = _db.Courses.AsQueryable();

        var totalCount = await query.CountAsync();

        var courses = await query
            .Include(c => c.Subjects)
            .Include(c => c.Enrollments)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CourseDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Description = c.Description,
                Capacity = c.Capacity,
                IsActive = c.IsActive,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                IsEnrolled = currentUserId.HasValue
                    ? c.Enrollments.Any(e => e.StudentId == currentUserId.Value)
                    : false,
                Subjects = c.Subjects.Select(s => new SubjectSummaryDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Code = s.Code,
                    Credits = s.Credits
                }).ToList()
            })
            .ToListAsync();

        return (courses, totalCount);
    }

    public async Task<CourseDto?> GetByIdAsync(Guid id)
    {
        return await _db.Courses
            .Include(c => c.Subjects)
            .Where(c => c.Id == id)
            .Select(c => new CourseDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Description = c.Description,
                Capacity = c.Capacity,
                IsActive = c.IsActive,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Subjects = c.Subjects.Select(s => new SubjectSummaryDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Code = s.Code,
                    Credits = s.Credits
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<CourseDto> CreateAsync(CreateCourseRequest request)
    {
        var course = new Course
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            Capacity = request.Capacity,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = true
        };

        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        return new CourseDto
        {
            Id = course.Id,
            Name = course.Name,
            Code = course.Code,
            Description = course.Description,
            Capacity = course.Capacity,
            IsActive = course.IsActive,
            StartDate = course.StartDate,
            EndDate = course.EndDate,
            Subjects = []
        };
    }

    public async Task UpdateAsync(Guid id, UpdateCourseRequest request)
    {
        var course = await _db.Courses.FindAsync(id)
            ?? throw new KeyNotFoundException($"Course '{id}' not found.");

        course.Name = request.Name ?? course.Name;
        course.Description = request.Description ?? course.Description;
        course.Capacity = request.Capacity ?? course.Capacity;
        course.IsActive = request.IsActive ?? course.IsActive;
        course.StartDate = request.StartDate ?? course.StartDate;
        course.EndDate = request.EndDate ?? course.EndDate;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var course = await _db.Courses.FindAsync(id)
            ?? throw new KeyNotFoundException($"Course '{id}' not found.");

        _db.Courses.Remove(course);
        await _db.SaveChangesAsync();
    }

    public async Task EnrollStudentAsync(Guid courseId, EnrollStudentRequest request)
    {
        var course = await _db.Courses.FindAsync(courseId)
            ?? throw new KeyNotFoundException("Course not found.");

        var student = await _db.Users.FindAsync(request.StudentId)
            ?? throw new KeyNotFoundException("Student not found.");

        var existing = await _db.Set<CourseEnrollment>()
            .FirstOrDefaultAsync(ce => ce.CourseId == courseId && ce.StudentId == request.StudentId);

        if (existing is not null)
            throw new InvalidOperationException("Student is already enrolled in this course.");

        _db.Set<CourseEnrollment>().Add(new CourseEnrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    public async Task StudentEnrollAsync(Guid courseId, Guid studentId)
    {
        var course = await _db.Courses.FindAsync(courseId)
            ?? throw new KeyNotFoundException("Course not found.");

        var now = DateTime.UtcNow;
        if (now < course.StartDate || now > course.EndDate)
            throw new InvalidOperationException("Course is not currently open for enrollment.");

        var existingEnrollments = await _db.Set<CourseEnrollment>()
            .Include(ce => ce.Course)
            .Where(ce => ce.StudentId == studentId)
            .ToListAsync();

        if (existingEnrollments.Any(ce => ce.CourseId == courseId))
            throw new InvalidOperationException("You are already enrolled in this course.");

        if (existingEnrollments.Any(ce => ce.Course.EndDate >= now))
            throw new InvalidOperationException(
                "You are currently enrolled in another active course. " +
                "You can only enroll in a new course after your current course ends.");

        _db.Set<CourseEnrollment>().Add(new CourseEnrollment
        {
            CourseId = courseId,
            StudentId = studentId,
            EnrolledAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<object>> GetEnrollmentsAsync(Guid courseId)
    {
        return await _db.Set<CourseEnrollment>()
            .Include(ce => ce.Student)
            .Where(ce => ce.CourseId == courseId)
            .Select(ce => (object)new
            {
                ce.StudentId,
                StudentName = ce.Student.Name,
                StudentEmail = ce.Student.Email,
                ce.EnrolledAt
            })
            .ToListAsync();
    }

    public async Task RemoveEnrollmentAsync(Guid courseId, Guid studentId)
    {
        var enrollment = await _db.Set<CourseEnrollment>()
            .FirstOrDefaultAsync(ce => ce.CourseId == courseId && ce.StudentId == studentId)
            ?? throw new KeyNotFoundException("Enrollment not found.");

        _db.Set<CourseEnrollment>().Remove(enrollment);
        await _db.SaveChangesAsync();
    }
}
