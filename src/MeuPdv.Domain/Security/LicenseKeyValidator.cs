using System.Security.Cryptography;
using System.Text;

namespace MeuPdv.Domain.Security;

public static class LicenseKeyValidator
{
    private const string Prefix = "MPDV";
    private static readonly byte[] Secret = Encoding.UTF8.GetBytes("MeuPDV-License-v1-Offline-Key");

    public static bool TryValidar(string chave, out DateTime validadeUtc, out string? erro)
    {
        validadeUtc = default;
        erro = null;

        if (string.IsNullOrWhiteSpace(chave))
        {
            erro = "Informe a chave de licença.";
            return false;
        }

        var partes = chave.Trim().ToUpperInvariant().Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length != 3 || partes[0] != Prefix)
        {
            erro = "Formato inválido. Use MPDV-AAAAMMDD-XXXXXXXX";
            return false;
        }

        if (!DateTime.TryParseExact(
                partes[1],
                "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var data))
        {
            erro = "Data de validade inválida na chave.";
            return false;
        }

        validadeUtc = new DateTime(data.Year, data.Month, data.Day, 23, 59, 59, DateTimeKind.Utc);

        var esperado = GerarChecksum(partes[1]);
        if (!string.Equals(partes[2], esperado, StringComparison.OrdinalIgnoreCase))
        {
            erro = "Chave de licença inválida.";
            return false;
        }

        return true;
    }

    public static string GerarChecksum(string dataValidade) =>
        Convert.ToHexString(HMACSHA256.HashData(Secret, Encoding.UTF8.GetBytes(dataValidade)))[..8];

    public static string GerarChaveDemonstracao(DateTime validadeUtc)
    {
        var data = validadeUtc.ToUniversalTime().ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        return $"{Prefix}-{data}-{GerarChecksum(data)}";
    }
}
