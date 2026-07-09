namespace MeuPdv.Domain.Rules;

public static class EstoqueCalculator
{
    public static (bool Permitido, string? Erro) ValidarMovimentacao(
        decimal saldoAtual,
        decimal delta,
        bool estoqueNegativoPermitido)
    {
        if (delta == 0)
            return (false, "Quantidade deve ser diferente de zero.");

        var saldoPosterior = saldoAtual + delta;
        if (!estoqueNegativoPermitido && saldoPosterior < 0)
            return (false, "Estoque insuficiente. Movimentação não permitida.");

        return (true, null);
    }

    public static decimal CalcularDeltaAjuste(decimal saldoAtual, decimal saldoFinal) =>
        saldoFinal - saldoAtual;
}
