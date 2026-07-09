using MeuPdv.Domain.Enums;

namespace MeuPdv.Domain.Rules;

public static class CaixaCalculator
{
    public static decimal CalcularEsperadoDinheiro(
        decimal valorInicial,
        decimal totalVendasDinheiro,
        decimal totalSuprimentos,
        decimal totalSangrias) =>
        Math.Round(valorInicial + totalVendasDinheiro + totalSuprimentos - totalSangrias, 2);

    public static decimal CalcularDiferenca(decimal valorContado, decimal valorEsperado) =>
        Math.Round(valorContado - valorEsperado, 2);

    public static Dictionary<FormaPagamento, decimal> AgruparPorForma(
        IEnumerable<(FormaPagamento Forma, decimal Valor)> movimentacoes)
    {
        return movimentacoes
            .GroupBy(m => m.Forma)
            .ToDictionary(g => g.Key, g => Math.Round(g.Sum(x => x.Valor), 2));
    }
}
