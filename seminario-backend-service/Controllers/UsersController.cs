using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seminario.Application.Common;
using Seminario.Application.DTOs;
using Seminario.Application.DTOs.Users;
using Seminario.Application.Interfaces;
using Seminario.Domain.Enums;

namespace Seminario.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = RoleName.Admin)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll([FromQuery] UserQueryDto query)
    {
        return Ok(await _userService.GetPagedAsync(query));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        return Ok(await _userService.GetByIdAsync(id));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto)
    {
        var created = await _userService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UpdateUserDto dto)
    {
        return Ok(await _userService.UpdateAsync(id, dto));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<UserDto>> ChangeStatus(int id, [FromBody] ChangeStatusDto dto)
    {
        return Ok(await _userService.UpdateAsync(id, new UpdateUserDto { Status = dto.Status }));
    }

    [HttpPatch("{id:int}/role")]
    public async Task<ActionResult<UserDto>> ChangeRole(int id, [FromBody] ChangeRoleDto dto)
    {
        return Ok(await _userService.UpdateAsync(id, new UpdateUserDto { Role = dto.Role }));
    }

    [HttpPatch("{id:int}/unlock")]
    public async Task<ActionResult<UserDto>> Unlock(int id)
    {
        return Ok(await _userService.UnlockAsync(id));
    }
}
