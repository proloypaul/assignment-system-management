using AssignmentSystem.Application.Features.Users.DTOs;
using AssignmentSystem.Application.Features.Users.Interfaces;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _db;

    public UserService(UserManager<User> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<(IEnumerable<UserDto> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? role, string? search)
    {
        var query = _userManager.Users.AsQueryable();

        // Role filter
        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleEntity = await _db.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.NormalizedName == role.ToUpperInvariant());

            if (roleEntity is null)
                return (Array.Empty<UserDto>(), 0);

            var idsInRole = _db.UserRoles
                .Where(ur => ur.RoleId == roleEntity.Id)
                .Select(ur => ur.UserId);

            query = query.Where(u => idsInRole.Contains(u.Id));
        }

        // Search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(u =>
                (u.Name != null && u.Name.ToLower().Contains(term)) ||
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync();

        var pageItems = await query
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var pageUserIds = pageItems.Select(u => u.Id).ToList();

        var roleMap = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => pageUserIds.Contains(ur.UserId))
            .Join(_db.Roles.AsNoTracking(),
                  ur => ur.RoleId,
                  r => r.Id,
                  (ur, r) => new { ur.UserId, RoleName = r.Name })
            .ToDictionaryAsync(x => x.UserId, x => x.RoleName);

        var result = pageItems.Select(u => new UserDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            ProfileImage = u.ProfileImage,
            CreatedAt = u.CreatedAt,
            Role = roleMap.TryGetValue(u.Id, out var roleName) && roleName != null ? roleName : "Unknown"
        }).ToList();

        return (result, totalCount);
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return null;

        var roleName = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == id)
            .Join(_db.Roles.AsNoTracking(),
                  ur => ur.RoleId,
                  r => r.Id,
                  (ur, r) => r.Name)
            .FirstOrDefaultAsync() ?? "Unknown";

        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            ProfileImage = user.ProfileImage,
            CreatedAt = user.CreatedAt,
            Role = roleName
        };
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            Name = request.Name,
            PhoneNumber = request.Phone
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, request.Role);

        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = request.Role
        };
    }

    public async Task UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
            ?? throw new KeyNotFoundException($"User '{id}' not found.");

        user.Name = request.Name ?? user.Name;
        user.PhoneNumber = request.Phone ?? user.PhoneNumber;
        user.ProfileImage = request.ProfileImage ?? user.ProfileImage;

        await _userManager.UpdateAsync(user);
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
            ?? throw new KeyNotFoundException($"User '{id}' not found.");

        await _userManager.DeleteAsync(user);
    }
}
