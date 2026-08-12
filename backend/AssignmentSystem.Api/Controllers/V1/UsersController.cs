using Asp.Versioning;
using AssignmentSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<User> _userManager;

    public UsersController(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>Get all users (Admin only).</summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        var users = _userManager.Users.Select(u => new
        {
            u.Id, u.Name, u.Email, u.PhoneNumber, u.ProfileImage, u.CreatedAt
        }).ToList();
        return Ok(users);
    }

    /// <summary>Get a single user by ID (Admin only).</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();
        return Ok(new { user.Id, user.Name, user.Email, user.PhoneNumber, user.ProfileImage, user.CreatedAt });
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
            return BadRequest(result.Errors);

        await _userManager.AddToRoleAsync(user, request.Role);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new { user.Id, user.Email, user.Name });
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

public record CreateUserRequest(string Name, string Email, string Password, string? Phone, string Role);
public record UpdateUserRequest(string? Name, string? Phone, string? ProfileImage);
