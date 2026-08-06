using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seminario.Api;
using Seminario.Application.DTOs;
using Seminario.Application.DTOs.Profile;
using Seminario.Application.Interfaces;

namespace Seminario.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<ActionResult<UserDto>> Get()
    {
        return Ok(await _profileService.GetAsync(User.GetUserId()));
    }

    [HttpPut]
    public async Task<ActionResult<UserDto>> Update([FromBody] UpdateProfileDto dto)
    {
        return Ok(await _profileService.UpdateAsync(User.GetUserId(), dto));
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        await _profileService.ChangePasswordAsync(User.GetUserId(), dto);
        return Ok(new { success = true, message = "Contrasena actualizada correctamente" });
    }
}
