using MeuPdv.Application.Licenca;

namespace MeuPdv.Application.Abstractions;

public interface ILicenseAppService
{
    Task<LicencaInfoDto> ObterInfoAsync(CancellationToken ct = default);
    Task<LicencaInfoDto> AtivarAsync(AtivarLicencaRequest request, CancellationToken ct = default);
    Task ExigirLicencaValidaParaOperacaoAsync(CancellationToken ct = default);
}
