namespace MeuPdv.Application.Abstractions;

public interface ILicenseService
{
    Task<LicenseStatus> ValidarAsync(CancellationToken ct = default);
    Task<LicenseStatus> AtivarLocalAsync(string chave, CancellationToken ct = default);
}

public record LicenseStatus(bool Valida, bool EmTolerancia, DateTime? Validade, string? Aviso);
