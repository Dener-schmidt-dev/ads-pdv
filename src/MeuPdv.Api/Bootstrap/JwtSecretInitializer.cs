using System.Security.Cryptography;
using System.Text;

namespace MeuPdv.Api.Bootstrap;

public static class JwtSecretInitializer
{
    public static string GarantirSecret(string dataDirectory, string? secretConfigurado, bool isDevelopment)
    {
        if (isDevelopment && !string.IsNullOrWhiteSpace(secretConfigurado))
            return secretConfigurado;

        Directory.CreateDirectory(dataDirectory);
        var secretPath = Path.Combine(dataDirectory, "jwt.secret");

        if (File.Exists(secretPath))
        {
            var existente = File.ReadAllText(secretPath).Trim();
            if (existente.Length >= 32)
                return existente;
        }

        var novo = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        File.WriteAllText(secretPath, novo, Encoding.UTF8);
        return novo;
    }
}
