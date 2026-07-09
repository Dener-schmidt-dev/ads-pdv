using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Rules;

namespace MeuPdv.Domain.Tests.Rules;

public class VendaCalculatorTests
{
    [Fact]
    public void CalcularSubtotal_IgnoraItensCancelados()
    {
        var itens = new List<VendaItem>
        {
            new() { Quantidade = 2, PrecoUnitario = 10m, Cancelado = false },
            new() { Quantidade = 1, PrecoUnitario = 5m, Cancelado = true }
        };

        Assert.Equal(20m, VendaCalculator.CalcularSubtotal(itens));
    }

    [Fact]
    public void CalcularTotal_NaoPermiteNegativo()
    {
        Assert.Equal(0m, VendaCalculator.CalcularTotal(10m, 15m));
        Assert.Equal(7.50m, VendaCalculator.CalcularTotal(10m, 2.50m));
    }

    [Fact]
    public void Recalcular_AtualizaVenda()
    {
        var venda = new Venda
        {
            Itens =
            [
                new VendaItem { Quantidade = 3, PrecoUnitario = 4.49m }
            ],
            Desconto = 1m
        };

        VendaCalculator.Recalcular(venda);

        Assert.Equal(13.47m, venda.Subtotal);
        Assert.Equal(12.47m, venda.Total);
    }
}
