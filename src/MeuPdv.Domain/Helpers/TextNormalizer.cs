using System.Globalization;
using System.Text;

namespace MeuPdv.Domain.Helpers;

public static class TextNormalizer
{
    public static string Normalizar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var normalized = texto.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant().Trim();
    }
}
