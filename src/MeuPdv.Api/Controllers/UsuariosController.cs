using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Auth;
using MeuPdv.Api.Extensions;
using MeuPdv.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuPdv.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioAppService _usuarioAppService;

    public UsuariosController(IUsuarioAppService usuarioAppService) =>
        _usuarioAppService = usuarioAppService;

    [HttpGet]
    [Authorize(Policy = nameof(Permissao.AcessarConfiguracoes))]
    public async Task<ActionResult<IReadOnlyList<UsuarioDto>>> Listar(CancellationToken ct) =>
        Ok(await _usuarioAppService.ListarAsync(ct));

    [HttpGet("{id}")]
    [Authorize(Policy = nameof(Permissao.AcessarConfiguracoes))]
    public async Task<ActionResult<UsuarioDto>> Obter(string id, CancellationToken ct)
    {
        var usuario = await _usuarioAppService.ObterPorIdAsync(id, ct);
        return usuario is null ? NotFound() : Ok(usuario);
    }

    [HttpPost]
    [Authorize(Policy = nameof(Permissao.AcessarConfiguracoes))]
    public async Task<ActionResult<UsuarioDto>> Criar([FromBody] CriarUsuarioRequest request, CancellationToken ct)
    {
        var operadorId = User.ObterUsuarioId();
        var usuario = await _usuarioAppService.CriarAsync(request, operadorId, ct);
        return CreatedAtAction(nameof(Obter), new { id = usuario.Id }, usuario);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = nameof(Permissao.AcessarConfiguracoes))]
    public async Task<ActionResult<UsuarioDto>> Atualizar(
        string id,
        [FromBody] AtualizarUsuarioRequest request,
        CancellationToken ct)
    {
        var operadorId = User.ObterUsuarioId();
        var usuario = await _usuarioAppService.AtualizarAsync(id, request, operadorId, ct);
        return Ok(usuario);
    }

    [HttpPost("{id}/inativar")]
    [Authorize(Policy = nameof(Permissao.AcessarConfiguracoes))]
    public async Task<IActionResult> Inativar(string id, CancellationToken ct)
    {
        var operadorId = User.ObterUsuarioId();
        await _usuarioAppService.InativarAsync(id, operadorId, ct);
        return NoContent();
    }
}
