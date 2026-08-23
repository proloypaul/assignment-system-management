using AssignmentSystem.Application.Features.Users.DTOs;

namespace AssignmentSystem.Application.Features.Users.Interfaces;

public interface IUserService
{
    Task<(IEnumerable<UserDto> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? role, string? search);
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<UserDto> CreateAsync(CreateUserRequest request);
    Task UpdateAsync(Guid id, UpdateUserRequest request);
    Task DeleteAsync(Guid id);
}
