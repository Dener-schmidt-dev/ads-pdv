using MeuPdv.Domain.Entities;

namespace MeuPdv.Domain.Rules;

public static class VendaCalculator
{
    public static decimal CalcularSubtotal(IEnumerable<VendaItem> itens) =>
        Math.Round(itens.Where(i => !i.Cancelado).Sum(i => i.Total), 2);

    public static decimal CalcularTotal(decimal subtotal, decimal desconto) =>
        Math.Round(Math.Max(0, subtotal - desconto), 2);

    public static void Recalcular(Venda venda)
    {
        venda.Subtotal = CalcularSubtotal(venda.Itens);
        venda.Total = CalcularTotal(venda.Subtotal, venda.Desconto);
    }
}
