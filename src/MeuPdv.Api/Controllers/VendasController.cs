using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Vendas;
using MeuPdv.Api.Extensions;
using MeuPdv.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuPdv.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VendasController : ControllerBase
{
    private readonly IVendaAppService _vendaAppService;

    public VendasController(IVendaAppService vendaAppService) => _vendaAppService = vendaAppService;

    [HttpGet("recentes")]
    public async Task<ActionResult<VendaListaResponse>> ListarRecentes(
        [FromQuery] string? termo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        CancellationToken ct = default) =>
        Ok(await _vendaAppService.ListarRecentesAsync(termo, pagina, tamanho, ct));

    [HttpGet("produto/{codigo}")]
    public async Task<ActionResult<ProdutoLeituraDto>> LerProduto(string codigo, CancellationToken ct)
    {
        var produto = await _vendaAppService.LerProdutoAsync(codigo, ct);
        return produto is null ? NotFound() : Ok(produto);
    }

    [HttpGet("busca-produtos")]
    public async Task<ActionResult<IReadOnlyList<ProdutoLeituraDto>>> BuscarProdutos(
        [FromQuery] string termo,
        [FromQuery] int limite = 10,
        CancellationToken ct = default) =>
        Ok(await _vendaAppService.BuscarProdutosAsync(termo, limite, ct));

    [HttpGet("detalhe/{id}")]
    public async Task<ActionResult<VendaDetalheDto>> Obter(string id, CancellationToken ct)
    {
        var venda = await _vendaAppService.ObterAsync(id, ct);
        return venda is null ? NotFound() : Ok(venda);
    }

    [HttpPost("finalizar")]
    public async Task<ActionResult<FinalizarVendaResponse>> Finalizar(
        [FromBody] FinalizarVendaRequest request,
        CancellationToken ct)
    {
        var podeConcederDesconto = User.HasClaim("perm", Permissao.ConcederDesconto.ToString());
        if (request.Desconto > 0 && !podeConcederDesconto)
            return Forbid();

        return Ok(await _vendaAppService.FinalizarAsync(
            request, User.ObterUsuarioId(), podeConcederDesconto, ct));
    }

    [HttpPost("cancelar/{id}")]
    [Authorize(Policy = nameof(Permissao.CancelarVenda))]
    public async Task<ActionResult<CancelarVendaResponse>> Cancelar(
        string id,
        [FromBody] CancelarVendaRequest request,
        CancellationToken ct) =>
        Ok(await _vendaAppService.CancelarAsync(id, request, User.ObterUsuarioId(), ct));
}
