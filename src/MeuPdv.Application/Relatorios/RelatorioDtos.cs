namespace MeuPdv.Application.Relatorios;

public record VendasPorPeriodoDto(
    DateTime Inicio,
    DateTime Fim,
    int QuantidadeVendas,
    decimal Subtotal,
    decimal Descontos,
    decimal Total,
    IReadOnlyList<ResumoFormaPagamentoDto> PorFormaPagamento,
    IReadOnlyList<VendaResumoDto> Vendas);

public record ResumoFormaPagamentoDto(string Forma, decimal Valor);

public record VendaResumoDto(
    string Id,
    string Numero,
    DateTime Data,
    decimal Subtotal,
    decimal Desconto,
    decimal Total,
    string FormasPagamento);

public record ProdutoMaisVendidoDto(
    string ProdutoId,
    string CodigoInterno,
    string Descricao,
    decimal Quantidade,
    decimal ValorTotal);

public record ProdutosMaisVendidosDto(
    DateTime Inicio,
    DateTime Fim,
    IReadOnlyList<ProdutoMaisVendidoDto> Itens);

public record EstoqueBaixoItemDto(
    string ProdutoId,
    string CodigoInterno,
    string Descricao,
    decimal EstoqueAtual,
    decimal EstoqueMinimo);
