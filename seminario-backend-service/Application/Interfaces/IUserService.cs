using Seminario.Application.Common;
using Seminario.Application.DTOs;
using Seminario.Application.DTOs.Users;

namespace Seminario.Application.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetPagedAsync(UserQueryDto query);
    Task<UserDto> GetByIdAsync(int id);
    Task<UserDto> CreateAsync(CreateUserDto dto);
    Task<UserDto> UpdateAsync(int id, UpdateUserDto dto);
    Task<UserDto> UnlockAsync(int id);
}
