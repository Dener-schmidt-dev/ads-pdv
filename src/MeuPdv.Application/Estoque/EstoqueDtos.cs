using MeuPdv.Domain.Enums;

namespace MeuPdv.Application.Estoque;

public record MovimentacaoEstoqueDto(
    string Id,
    string ProdutoId,
    string ProdutoDescricao,
    OrigemMovimentacaoEstoque Origem,
    decimal Quantidade,
    decimal SaldoAnterior,
    decimal SaldoPosterior,
    string? Motivo,
    DateTime CreatedAt);

public record MovimentacaoManualRequest(
    string ProdutoId,
    decimal Quantidade,
    string Motivo);

public record AjusteEstoqueRequest(
    string ProdutoId,
    decimal SaldoFinal,
    string Motivo);

public record AlertaEstoqueDto(
    string ProdutoId,
    string CodigoInterno,
    string Descricao,
    decimal EstoqueAtual,
    decimal EstoqueMinimo);

public record SaldoProdutoDto(
    string ProdutoId,
    string CodigoInterno,
    string Descricao,
    decimal EstoqueAtual,
    decimal EstoqueMinimo,
    bool AbaixoMinimo);
