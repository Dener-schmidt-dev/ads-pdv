using MeuPdv.Domain.Enums;
using MeuPdv.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuPdv.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PerfisController : ControllerBase
{
    [HttpGet]
    public ActionResult<object> Listar()
    {
        var perfis = Enum.GetValues<PerfilUsuario>().Select(p => new
        {
            valor = (int)p,
            nome = p.ToString(),
            permissoes = PermissaoPorPerfil.ObterPermissoes(p)
        });

        return Ok(perfis);
    }

    [HttpGet("permissoes")]
    public ActionResult<object> ListarPermissoes() =>
        Ok(Enum.GetValues<Permissao>().Select(p => new { valor = p.ToString(), nome = p.ToString() }));
}
