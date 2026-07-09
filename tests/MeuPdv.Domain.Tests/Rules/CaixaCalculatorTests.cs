using MeuPdv.Domain.Enums;
using MeuPdv.Domain.Rules;

namespace MeuPdv.Domain.Tests.Rules;

public class CaixaCalculatorTests
{
    [Fact]
    public void CalcularEsperadoDinheiro_SomaCorretamente()
    {
        var esperado = CaixaCalculator.CalcularEsperadoDinheiro(100m, 250m, 50m, 30m);
        Assert.Equal(370m, esperado);
    }

    [Fact]
    public void CalcularDiferenca_RetornaContadoMenosEsperado()
    {
        Assert.Equal(5m, CaixaCalculator.CalcularDiferenca(375m, 370m));
        Assert.Equal(-10m, CaixaCalculator.CalcularDiferenca(360m, 370m));
    }

    [Fact]
    public void AgruparPorForma_AgrupaValores()
    {
        var dados = new[]
        {
            (FormaPagamento.Dinheiro, 100m),
            (FormaPagamento.Pix, 50m),
            (FormaPagamento.Dinheiro, 25m)
        };

        var grupo = CaixaCalculator.AgruparPorForma(dados);

        Assert.Equal(125m, grupo[FormaPagamento.Dinheiro]);
        Assert.Equal(50m, grupo[FormaPagamento.Pix]);
    }
}
