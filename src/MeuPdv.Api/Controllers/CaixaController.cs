using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Caixa;
using MeuPdv.Api.Extensions;
using MeuPdv.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuPdv.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CaixaController : ControllerBase
{
    private readonly ICaixaAppService _caixaAppService;

    public CaixaController(ICaixaAppService caixaAppService) => _caixaAppService = caixaAppService;

    [HttpGet("atual")]
    public async Task<ActionResult<SessaoCaixaDto>> ObterAtual(
        [FromQuery] string caixa = "CAIXA-01",
        CancellationToken ct = default)
    {
        var sessao = await _caixaAppService.ObterSessaoAbertaAsync(caixa, ct);
        return sessao is null ? NotFound() : Ok(sessao);
    }

    [HttpPost("abrir")]
    [Authorize(Policy = nameof(Permissao.AbrirCaixa))]
    public async Task<ActionResult<SessaoCaixaDto>> Abrir(
        [FromBody] AbrirCaixaRequest request,
        CancellationToken ct)
    {
        var sessao = await _caixaAppService.AbrirAsync(request, User.ObterUsuarioId(), ct);
        return Ok(sessao);
    }

    [HttpPost("suprimento")]
    [Authorize(Policy = nameof(Permissao.AbrirCaixa))]
    public async Task<ActionResult<MovimentacaoCaixaDto>> Suprimento(
        [FromBody] SuprimentoSangriaRequest request,
        CancellationToken ct) =>
        Ok(await _caixaAppService.SuprimentoAsync(request, User.ObterUsuarioId(), ct));

    [HttpPost("sangria")]
    [Authorize(Policy = nameof(Permissao.AbrirCaixa))]
    public async Task<ActionResult<MovimentacaoCaixaDto>> Sangria(
        [FromBody] SuprimentoSangriaRequest request,
        CancellationToken ct) =>
        Ok(await _caixaAppService.SangriaAsync(request, User.ObterUsuarioId(), ct));

    [HttpPost("fechar")]
    [Authorize(Policy = nameof(Permissao.FecharCaixa))]
    public async Task<ActionResult<ResumoFechamentoDto>> Fechar(
        [FromBody] FecharCaixaRequest request,
        CancellationToken ct) =>
        Ok(await _caixaAppService.FecharAsync(request, User.ObterUsuarioId(), ct));

    [HttpGet("historico")]
    public async Task<ActionResult<IReadOnlyList<SessaoCaixaDto>>> Historico(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        CancellationToken ct = default) =>
        Ok(await _caixaAppService.HistoricoAsync(pagina, tamanho, ct));
}
