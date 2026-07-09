using System.Globalization;

namespace MeuPdv.Application.Helpers;

public static class BrlFormatter
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string Moeda(decimal valor) => valor.ToString("C", PtBr);

    public static string Moeda(decimal? valor) =>
        valor.HasValue ? Moeda(valor.Value) : Moeda(0);

    public static string Decimal(decimal valor, int casas = 2) =>
        valor.ToString($"N{casas}", PtBr);

    public static string Percentual(decimal valor, int casas = 2) =>
        $"{valor.ToString($"N{casas}", PtBr)}%";
}
