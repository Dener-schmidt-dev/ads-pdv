using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Configuracoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuPdv.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PixController : ControllerBase
{
    private readonly IPixQrService _pixQrService;

    public PixController(IPixQrService pixQrService) => _pixQrService = pixQrService;

    [HttpPost("qr")]
    public async Task<ActionResult<PixQrResultado>> GerarQr([FromBody] GerarPixQrRequest request, CancellationToken ct)
    {
        if (request.Valor <= 0)
            return BadRequest(new { erro = "Valor deve ser maior que zero." });

        var id = string.IsNullOrWhiteSpace(request.Identificador)
            ? $"PDV-{DateTime.UtcNow:yyyyMMddHHmmss}"
            : request.Identificador.Trim();

        var desc = string.IsNullOrWhiteSpace(request.Descricao) ? "Pagamento MeuPDV" : request.Descricao.Trim();

        var resultado = await _pixQrService.GerarAsync(request.Valor, id, desc, ct);
        return Ok(resultado);
    }
}
