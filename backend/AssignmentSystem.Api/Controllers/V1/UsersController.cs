using Asp.Versioning;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _db;

    public UsersController(UserManager<User> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    /// <summary>
    /// Get all users with pagination, role filter, and search.
    /// Uses a single JOIN query for roles — no concurrent DbContext usage.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? role = null,
        [FromQuery] string? search = null)
    {
        var query = _userManager.Users.AsQueryable();

        // ── Role filter ────────────────────────────────────────────────────────
        // Get matching user IDs from the UserRoles JOIN — one query, no N+1.
        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleEntity = await _db.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.NormalizedName == role.ToUpperInvariant());

            if (roleEntity is null)
                return Ok(new { items = Array.Empty<object>(), totalCount = 0, page, pageSize, totalPages = 0 });

            var idsInRole = _db.UserRoles
                .Where(ur => ur.RoleId == roleEntity.Id)
                .Select(ur => ur.UserId);

            query = query.Where(u => idsInRole.Contains(u.Id));
        }

        // ── Search ────────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(u =>
                (u.Name != null && u.Name.ToLower().Contains(term)) ||
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(term)));
        }

        // ── Pagination ────────────────────────────────────────────────────────
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pageItems = await query
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // ── Roles — ONE join query for the whole page ─────────────────────────
        var pageUserIds = pageItems.Select(u => u.Id).ToList();

        var roleMap = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => pageUserIds.Contains(ur.UserId))
            .Join(_db.Roles.AsNoTracking(),
                  ur => ur.RoleId,
                  r => r.Id,
                  (ur, r) => new { ur.UserId, RoleName = r.Name })
            .ToDictionaryAsync(x => x.UserId, x => x.RoleName);

        // ── Project to DTO ────────────────────────────────────────────────────
        var result = pageItems.Select(u => new UserDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            ProfileImage = u.ProfileImage,
            CreatedAt = u.CreatedAt,
            Role = roleMap.TryGetValue(u.Id, out var roleName) ? roleName : "Unknown"
        }).ToList();

        return Ok(new { items = result, totalCount, page, pageSize, totalPages });
    }

    /// <summary>Get a single user by ID (Admin only).</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        // Single JOIN query for this user's role
        var roleName = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == id)
            .Join(_db.Roles.AsNoTracking(),
                  ur => ur.RoleId,
                  r => r.Id,
                  (ur, r) => r.Name)
            .FirstOrDefaultAsync() ?? "Unknown";

        return Ok(new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            ProfileImage = user.ProfileImage,
            CreatedAt = user.CreatedAt,
            Role = roleName
        });
    }

    /// <summary>Create a new user with a specified role (Admin only).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
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
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

        await _userManager.AddToRoleAsync(user, request.Role);

        return CreatedAtAction(nameof(GetById), new { id = user.Id },
            new UserDto { Id = user.Id, Name = user.Name, Email = user.Email, Role = request.Role });
    }

    /// <summary>Update a user's profile (Admin only).</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        user.Name = request.Name ?? user.Name;
        user.PhoneNumber = request.Phone ?? user.PhoneNumber;
        user.ProfileImage = request.ProfileImage ?? user.ProfileImage;

        await _userManager.UpdateAsync(user);
        return NoContent();
    }

    /// <summary>Delete a user (Admin only).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        await _userManager.DeleteAsync(user);
        return NoContent();
    }
}

// ─── DTO ──────────────────────────────────────────────────────────────────────
public class UserDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ProfileImage { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Role { get; set; } = string.Empty;
}

// ─── Request records ──────────────────────────────────────────────────────────
public record CreateUserRequest(string Name, string Email, string Password, string? Phone, string Role);
public record UpdateUserRequest(string? Name, string? Phone, string? ProfileImage);
