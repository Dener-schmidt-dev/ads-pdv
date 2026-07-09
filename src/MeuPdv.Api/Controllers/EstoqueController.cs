using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Estoque;
using MeuPdv.Api.Extensions;
using MeuPdv.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuPdv.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EstoqueController : ControllerBase
{
    private readonly IEstoqueAppService _estoqueAppService;

    public EstoqueController(IEstoqueAppService estoqueAppService) => _estoqueAppService = estoqueAppService;

    [HttpGet("saldo/{produtoId}")]
    public async Task<ActionResult<SaldoProdutoDto>> ConsultarSaldo(string produtoId, CancellationToken ct)
    {
        var saldo = await _estoqueAppService.ConsultarSaldoAsync(produtoId, ct);
        return saldo is null ? NotFound() : Ok(saldo);
    }

    [HttpGet("saldos")]
    public async Task<ActionResult<IReadOnlyList<SaldoProdutoDto>>> ListarSaldos(
        [FromQuery] string? termo,
        [FromQuery] bool? abaixoMinimo,
        CancellationToken ct) =>
        Ok(await _estoqueAppService.ListarSaldosAsync(termo, abaixoMinimo, ct));

    [HttpGet("alertas")]
    public async Task<ActionResult<IReadOnlyList<AlertaEstoqueDto>>> Alertas(CancellationToken ct) =>
        Ok(await _estoqueAppService.ListarAlertasMinimoAsync(ct));

    [HttpGet("historico")]
    public async Task<ActionResult<IReadOnlyList<MovimentacaoEstoqueDto>>> Historico(
        [FromQuery] string? produtoId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 50,
        CancellationToken ct = default) =>
        Ok(await _estoqueAppService.HistoricoAsync(produtoId, pagina, tamanho, ct));

    [HttpPost("entrada")]
    [Authorize(Policy = nameof(Permissao.MovimentarEstoque))]
    public async Task<ActionResult<MovimentacaoEstoqueDto>> Entrada(
        [FromBody] MovimentacaoManualRequest request, CancellationToken ct) =>
        Ok(await _estoqueAppService.RegistrarEntradaAsync(request, User.ObterUsuarioId(), ct));

    [HttpPost("saida")]
    [Authorize(Policy = nameof(Permissao.MovimentarEstoque))]
    public async Task<ActionResult<MovimentacaoEstoqueDto>> Saida(
        [FromBody] MovimentacaoManualRequest request, CancellationToken ct) =>
        Ok(await _estoqueAppService.RegistrarSaidaAsync(request, User.ObterUsuarioId(), ct));

    [HttpPost("ajuste")]
    [Authorize(Policy = nameof(Permissao.MovimentarEstoque))]
    public async Task<ActionResult<MovimentacaoEstoqueDto>> Ajuste(
        [FromBody] AjusteEstoqueRequest request, CancellationToken ct) =>
        Ok(await _estoqueAppService.RegistrarAjusteAsync(request, User.ObterUsuarioId(), ct));
}
