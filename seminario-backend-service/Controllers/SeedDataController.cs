using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seminario.Api;
using Seminario.Application.DTOs.Operaciones;
using Seminario.Application.Interfaces;
using Seminario.Domain.Enums;

namespace Seminario.Controllers;

/// <summary>
/// Stress test de particionamiento
/// </summary>
[ApiController]
[Route("api/seed-data")]
[Authorize(Roles = RoleName.Admin)]
public class SeedDataController : ControllerBase
{
    private readonly ISeedDataService _seedDataService;

    public SeedDataController(ISeedDataService seedDataService)
    {
        _seedDataService = seedDataService;
    }

    [HttpPost]
    public async Task<ActionResult<SeedDataResultDto>> Inyectar(
        [FromBody] SeedDataRequestDto request,
        CancellationToken cancellationToken)
    {
        var resultado = await _seedDataService.InyectarAsync(User.GetUserId(), request, cancellationToken);
        return Ok(resultado);
    }
}
