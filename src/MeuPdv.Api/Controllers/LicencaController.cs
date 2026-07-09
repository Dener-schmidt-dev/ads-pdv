using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Licenca;
using MeuPdv.Domain.Enums;
using MeuPdv.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuPdv.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LicencaController : ControllerBase
{
    private readonly ILicenseAppService _licenseAppService;
    private readonly IWebHostEnvironment _environment;

    public LicencaController(ILicenseAppService licenseAppService, IWebHostEnvironment environment)
    {
        _licenseAppService = licenseAppService;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<LicencaInfoDto>> Obter(CancellationToken ct) =>
        Ok(await _licenseAppService.ObterInfoAsync(ct));

    [HttpPost("ativar")]
    [Authorize(Policy = nameof(Permissao.AcessarConfiguracoes))]
    public async Task<ActionResult<LicencaInfoDto>> Ativar(
        [FromBody] AtivarLicencaRequest request,
        CancellationToken ct) =>
        Ok(await _licenseAppService.AtivarAsync(request, ct));

    [HttpGet("gerar-demo")]
    [Authorize(Policy = nameof(Permissao.AcessarConfiguracoes))]
    public ActionResult<GerarChaveDemoResponse> GerarDemo()
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        var validade = DateTime.UtcNow.AddYears(1);
        return Ok(new GerarChaveDemoResponse(LicenseKeyValidator.GerarChaveDemonstracao(validade), validade));
    }
}
