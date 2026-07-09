using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Configuracoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuPdv.Api.Controllers;

[ApiController]
[Route("api/pdv")]
[Authorize]
public class PdvController : ControllerBase
{
    private readonly IConfiguracaoAppService _configuracaoAppService;

    public PdvController(IConfiguracaoAppService configuracaoAppService) =>
        _configuracaoAppService = configuracaoAppService;

    [HttpGet("visual")]
    public async Task<ActionResult<ConfiguracaoPdvDto>> ObterVisual(CancellationToken ct) =>
        Ok(await _configuracaoAppService.ObterPdvAsync(ct));

    [HttpGet("imagem-fundo")]
    public async Task<IActionResult> ObterImagemFundo(CancellationToken ct)
    {
        var arquivo = await _configuracaoAppService.ObterImagemFundoPdvAsync(ct);
        if (arquivo is null) return NotFound();
        return PhysicalFile(arquivo.Value.Caminho, arquivo.Value.ContentType);
    }
}
