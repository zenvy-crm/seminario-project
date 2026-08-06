using Seminario.Application.DTOs;
using Seminario.Application.DTOs.Profile;

namespace Seminario.Application.Interfaces;

public interface IProfileService
{
    Task<UserDto> GetAsync(int userId);
    Task<UserDto> UpdateAsync(int userId, UpdateProfileDto dto);
    Task ChangePasswordAsync(int userId, ChangePasswordDto dto);
}
