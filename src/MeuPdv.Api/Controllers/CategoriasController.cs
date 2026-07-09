using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Produtos;
using MeuPdv.Api.Extensions;
using MeuPdv.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuPdv.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriasController : ControllerBase
{
    private readonly ICategoriaAppService _categoriaAppService;

    public CategoriasController(ICategoriaAppService categoriaAppService) =>
        _categoriaAppService = categoriaAppService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoriaDto>>> Listar(CancellationToken ct) =>
        Ok(await _categoriaAppService.ListarAsync(ct));

    [HttpPost]
    [Authorize(Policy = nameof(Permissao.CadastrarProduto))]
    public async Task<ActionResult<CategoriaDto>> Criar([FromBody] CriarCategoriaRequest request, CancellationToken ct)
    {
        var categoria = await _categoriaAppService.CriarAsync(request, User.ObterUsuarioId(), ct);
        return Ok(categoria);
    }
}
