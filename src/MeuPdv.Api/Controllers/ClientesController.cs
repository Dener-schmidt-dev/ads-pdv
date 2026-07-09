using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Clientes;
using MeuPdv.Application.Vendas;
using MeuPdv.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuPdv.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientesController : ControllerBase
{
    private readonly IClienteAppService _clienteAppService;

    public ClientesController(IClienteAppService clienteAppService) =>
        _clienteAppService = clienteAppService;

    [HttpGet]
    public async Task<ActionResult<ClienteListaResponse>> Listar(
        [FromQuery] string? termo,
        [FromQuery] bool? ativo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 50,
        CancellationToken ct = default) =>
        Ok(await _clienteAppService.ListarAsync(termo, ativo, pagina, tamanho, ct));

    [HttpGet("busca")]
    public async Task<ActionResult<IReadOnlyList<ClienteDto>>> Buscar(
        [FromQuery] string termo,
        CancellationToken ct) =>
        Ok(await _clienteAppService.BuscarAsync(termo, ct));

    [HttpPost("rapido")]
    public async Task<ActionResult<ClienteDto>> CriarRapido(
        [FromBody] CriarClienteRapidoRequest request,
        CancellationToken ct) =>
        Ok(await _clienteAppService.CriarRapidoAsync(request, User.ObterUsuarioId(), ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<ClienteDetalheDto>> Obter(string id, CancellationToken ct)
    {
        var cliente = await _clienteAppService.ObterPorIdAsync(id, ct);
        return cliente is null ? NotFound() : Ok(cliente);
    }

    [HttpPost]
    public async Task<ActionResult<ClienteDetalheDto>> Criar(
        [FromBody] CriarClienteRequest request,
        CancellationToken ct)
    {
        var cliente = await _clienteAppService.CriarAsync(request, User.ObterUsuarioId(), ct);
        return CreatedAtAction(nameof(Obter), new { id = cliente.Id }, cliente);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ClienteDetalheDto>> Atualizar(
        string id,
        [FromBody] AtualizarClienteRequest request,
        CancellationToken ct) =>
        Ok(await _clienteAppService.AtualizarAsync(id, request, User.ObterUsuarioId(), ct));

    [HttpPost("{id}/inativar")]
    public async Task<IActionResult> Inativar(string id, CancellationToken ct)
    {
        await _clienteAppService.InativarAsync(id, User.ObterUsuarioId(), ct);
        return NoContent();
    }
}
