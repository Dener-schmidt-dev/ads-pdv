using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Backup;
using MeuPdv.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuPdv.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = nameof(Permissao.AcessarConfiguracoes))]
public class BackupController : ControllerBase
{
    private readonly IBackupAppService _backupAppService;

    public BackupController(IBackupAppService backupAppService) => _backupAppService = backupAppService;

    [HttpGet("status")]
    public async Task<ActionResult<BackupStatusDto>> ObterStatus(CancellationToken ct) =>
        Ok(await _backupAppService.ObterStatusAsync(ct));

    [HttpGet("historico")]
    public async Task<ActionResult<IReadOnlyList<BackupHistoricoDto>>> ListarHistorico(CancellationToken ct) =>
        Ok(await _backupAppService.ListarHistoricoAsync(ct));

    [HttpPost("executar")]
    public async Task<ActionResult<ExecutarBackupResultadoDto>> Executar(CancellationToken ct)
    {
        var resultado = await _backupAppService.ExecutarManualAsync(ct);
        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Mensagem ?? "Falha ao executar backup." });

        return Ok(resultado);
    }

    [HttpPost("restaurar/{id}")]
    public async Task<ActionResult<ExecutarBackupResultadoDto>> Restaurar(
        string id,
        [FromBody] RestaurarBackupRequest request,
        CancellationToken ct)
    {
        var resultado = await _backupAppService.RestaurarAsync(id, request, ct);
        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Mensagem ?? "Falha ao restaurar backup." });

        return Ok(resultado);
    }
}
