using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Enums;

namespace MeuPdv.Domain.Rules;

public static class PagamentoValidator
{
    public static (bool Valido, string? Erro) Validar(decimal totalVenda, IReadOnlyList<Pagamento> pagamentos)
    {
        if (pagamentos.Count == 0)
            return (false, "Informe ao menos uma forma de pagamento.");

        var totalPago = Math.Round(pagamentos.Sum(p => p.Valor), 2);

        if (totalPago < totalVenda)
            return (false, "Valor pago inferior ao total da venda.");

        var excesso = totalPago - totalVenda;
        if (excesso > 0)
        {
            var temDinheiro = pagamentos.Any(p => p.Forma == FormaPagamento.Dinheiro);
            if (!temDinheiro)
                return (false, "Excesso de pagamento só é permitido com troco em dinheiro.");
        }

        foreach (var pagamento in pagamentos.Where(p => p.Forma == FormaPagamento.Dinheiro))
        {
            if (pagamento.ValorRecebido < pagamento.Valor)
                return (false, "Valor recebido em dinheiro insuficiente.");
        }

        return (true, null);
    }

    public static decimal CalcularTroco(decimal valorRecebido, decimal valorPagamento) =>
        Math.Round(Math.Max(0, valorRecebido - valorPagamento), 2);
}
