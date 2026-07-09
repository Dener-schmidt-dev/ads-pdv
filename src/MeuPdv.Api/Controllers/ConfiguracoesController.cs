using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Configuracoes;
using MeuPdv.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuPdv.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = nameof(Permissao.AcessarConfiguracoes))]
public class ConfiguracoesController : ControllerBase
{
    private readonly IConfiguracaoAppService _configuracaoAppService;

    public ConfiguracoesController(IConfiguracaoAppService configuracaoAppService) =>
        _configuracaoAppService = configuracaoAppService;

    [HttpGet("empresa")]
    public async Task<ActionResult<ConfiguracaoEmpresaDto>> ObterEmpresa(CancellationToken ct) =>
        Ok(await _configuracaoAppService.ObterEmpresaAsync(ct));

    [HttpPut("empresa")]
    public async Task<ActionResult<ConfiguracaoEmpresaDto>> AtualizarEmpresa(
        [FromBody] AtualizarConfiguracaoEmpresaRequest request,
        CancellationToken ct) =>
        Ok(await _configuracaoAppService.AtualizarEmpresaAsync(request, ct));

    [HttpGet("sistema")]
    public async Task<ActionResult<ConfiguracaoSistemaDto>> ObterSistema(CancellationToken ct) =>
        Ok(await _configuracaoAppService.ObterSistemaAsync(ct));

    [HttpPut("sistema")]
    public async Task<ActionResult<ConfiguracaoSistemaDto>> AtualizarSistema(
        [FromBody] AtualizarConfiguracaoSistemaRequest request,
        CancellationToken ct) =>
        Ok(await _configuracaoAppService.AtualizarSistemaAsync(request, ct));

    [HttpGet("pdv")]
    public async Task<ActionResult<ConfiguracaoPdvDto>> ObterPdv(CancellationToken ct) =>
        Ok(await _configuracaoAppService.ObterPdvAsync(ct));

    [HttpPut("pdv")]
    public async Task<ActionResult<ConfiguracaoPdvDto>> AtualizarPdv(
        [FromBody] AtualizarConfiguracaoPdvRequest request,
        CancellationToken ct) =>
        Ok(await _configuracaoAppService.AtualizarPdvAsync(request, ct));

    [HttpPost("pdv/imagem-fundo")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<ActionResult<ConfiguracaoPdvDto>> SalvarImagemFundoPdv(IFormFile arquivo, CancellationToken ct)
    {
        if (arquivo.Length == 0)
            return BadRequest(new { erro = "Arquivo vazio." });

        await using var stream = arquivo.OpenReadStream();
        return Ok(await _configuracaoAppService.SalvarImagemFundoPdvAsync(
            stream, arquivo.Length, arquivo.ContentType, arquivo.FileName, ct));
    }
}
