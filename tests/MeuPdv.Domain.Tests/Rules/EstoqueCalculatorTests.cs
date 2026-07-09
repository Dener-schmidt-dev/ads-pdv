using MeuPdv.Domain.Rules;

namespace MeuPdv.Domain.Tests.Rules;

public class EstoqueCalculatorTests
{
    [Fact]
    public void ValidarMovimentacao_BloqueiaNegativo_QuandoNaoPermitido()
    {
        var (permitido, erro) = EstoqueCalculator.ValidarMovimentacao(5m, -10m, false);

        Assert.False(permitido);
        Assert.NotNull(erro);
    }

    [Fact]
    public void ValidarMovimentacao_PermiteNegativo_QuandoConfigurado()
    {
        var (permitido, _) = EstoqueCalculator.ValidarMovimentacao(5m, -10m, true);

        Assert.True(permitido);
    }

    [Fact]
    public void CalcularDeltaAjuste_RetornaDiferenca()
    {
        Assert.Equal(3m, EstoqueCalculator.CalcularDeltaAjuste(10m, 13m));
        Assert.Equal(-4m, EstoqueCalculator.CalcularDeltaAjuste(10m, 6m));
    }
}
