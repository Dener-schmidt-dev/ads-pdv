namespace MeuPdv.Domain.Rules;

public static class DescontoValidator
{
    public static (bool Permitido, bool ExigeAutorizacao, string? Erro) Validar(
        decimal subtotal,
        decimal desconto,
        decimal limitePercentualOperador)
    {
        if (desconto < 0)
            return (false, false, "Desconto não pode ser negativo.");

        if (subtotal <= 0)
            return (false, false, "Subtotal inválido para aplicar desconto.");

        if (desconto > subtotal)
            return (false, false, "Desconto não pode ser maior que o subtotal.");

        var percentual = desconto / subtotal * 100m;
        if (percentual > limitePercentualOperador)
            return (true, true, null);

        return (true, false, null);
    }
}
