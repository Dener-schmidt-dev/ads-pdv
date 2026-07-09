using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Auth;
using MeuPdv.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuPdv.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var resultado = await _authService.LoginAsync(request, ct);
        if (resultado is null)
            return Unauthorized(new { erro = "Usuário ou senha inválidos." });

        return Ok(resultado);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UsuarioLogadoDto>> Me(CancellationToken ct)
    {
        var usuarioId = User.ObterUsuarioId();
        var usuario = await _authService.ObterUsuarioLogadoAsync(usuarioId, ct);
        if (usuario is null)
            return Unauthorized();

        return Ok(usuario);
    }

    [HttpPost("alterar-senha")]
    [Authorize]
    public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaRequest request, CancellationToken ct)
    {
        await _authService.AlterarSenhaAsync(User.ObterUsuarioId(), request, ct);
        return NoContent();
    }
}
