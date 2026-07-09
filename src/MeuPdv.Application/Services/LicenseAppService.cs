using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Licenca;
using MeuPdv.Application.Options;
using MeuPdv.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace MeuPdv.Application.Services;

public class LicenseAppService : ILicenseAppService
{
    private readonly ILicenseService _licenseService;
    private readonly LicenseOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    public LicenseAppService(
        ILicenseService licenseService,
        IOptions<LicenseOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _licenseService = licenseService;
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<LicencaInfoDto> ObterInfoAsync(CancellationToken ct = default)
    {
        var status = await _licenseService.ValidarAsync(ct);
        return Mapear(status, null, null);
    }

    public async Task<LicencaInfoDto> AtivarAsync(AtivarLicencaRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Chave))
            throw new DomainException("Informe a chave de licença.");

        LicenseStatus status;

        if (!string.IsNullOrWhiteSpace(_options.ValidationUrl))
        {
            status = await TentarAtivarOnlineAsync(request.Chave.Trim(), ct)
                     ?? await _licenseService.AtivarLocalAsync(request.Chave, ct);
        }
        else
        {
            status = await _licenseService.AtivarLocalAsync(request.Chave, ct);
        }

        if (!status.Valida && !status.EmTolerancia)
            throw new DomainException(status.Aviso ?? "Não foi possível ativar a licença.");

        return Mapear(status, request.Chave, string.IsNullOrWhiteSpace(_options.ValidationUrl) ? "Local" : "Online");
    }

    public async Task ExigirLicencaValidaParaOperacaoAsync(CancellationToken ct = default)
    {
        var status = await _licenseService.ValidarAsync(ct);
        if (!status.Valida)
            throw new DomainException(status.Aviso ?? "Licença inválida ou expirada.");
    }

    private async Task<LicenseStatus?> TentarAtivarOnlineAsync(string chave, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("MeuPdvLicense");
            var response = await client.PostAsJsonAsync(
                _options.ValidationUrl!,
                new { chave, machineId = ObterMachineId() },
                ct);

            if (!response.IsSuccessStatusCode)
                return null;

            var body = await response.Content.ReadFromJsonAsync<OnlineLicenseResponse>(ct);
            if (body?.Valida != true || body.Validade is null)
                return null;

            return await _licenseService.AtivarLocalAsync(chave, ct);
        }
        catch
        {
            return null;
        }
    }

    private static LicencaInfoDto Mapear(LicenseStatus status, string? chave, string? tipo) => new(
        status.Valida,
        status.EmTolerancia,
        status.Validade,
        status.Aviso,
        MascararChave(chave),
        status.Valida ? DateTime.UtcNow : null,
        tipo ?? "—");

    private static string? MascararChave(string? chave)
    {
        if (string.IsNullOrWhiteSpace(chave) || chave.Length < 8)
            return null;
        return $"{chave[..4]}…{chave[^4..]}";
    }

    private static string ObterMachineId()
    {
        var raw = $"{Environment.MachineName}|{Environment.UserName}|{Environment.OSVersion}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..16];
    }

    private sealed record OnlineLicenseResponse(bool Valida, DateTime? Validade);
}
