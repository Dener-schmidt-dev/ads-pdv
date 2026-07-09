using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Enums;
using MeuPdv.Domain.Rules;

namespace MeuPdv.Domain.Tests.Rules;

public class PagamentoValidatorTests
{
    [Fact]
    public void Validar_RejeitaPagamentoInsuficiente()
    {
        var pagamentos = new List<Pagamento>
        {
            new() { Forma = FormaPagamento.Pix, Valor = 50m }
        };

        var (valido, erro) = PagamentoValidator.Validar(100m, pagamentos);

        Assert.False(valido);
        Assert.NotNull(erro);
    }

    [Fact]
    public void Validar_PermiteTrocoEmDinheiro()
    {
        var pagamentos = new List<Pagamento>
        {
            new() { Forma = FormaPagamento.Dinheiro, Valor = 100m, ValorRecebido = 120m, Troco = 20m }
        };

        var (valido, _) = PagamentoValidator.Validar(100m, pagamentos);

        Assert.True(valido);
    }

    [Fact]
    public void CalcularTroco_RetornaDiferenca()
    {
        Assert.Equal(15m, PagamentoValidator.CalcularTroco(115m, 100m));
        Assert.Equal(0m, PagamentoValidator.CalcularTroco(90m, 100m));
    }
}
