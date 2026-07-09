using MeuPdv.Domain.Enums;

namespace MeuPdv.Application.Caixa;

public record AbrirCaixaRequest(
    string CaixaIdentificador,
    decimal ValorInicial);

public record SuprimentoSangriaRequest(
    string SessaoCaixaId,
    decimal Valor,
    string Motivo);

public record FecharCaixaRequest(
    string SessaoCaixaId,
    decimal ValorContado,
    string? Observacao);

public record SessaoCaixaDto(
    string Id,
    string CaixaIdentificador,
    string OperadorAberturaId,
    StatusSessaoCaixa Status,
    decimal ValorInicial,
    DateTime AbertaEm,
    DateTime? FechadaEm);

public record MovimentacaoCaixaDto(
    string Id,
    string SessaoCaixaId,
    TipoMovimentacaoCaixa Tipo,
    decimal Valor,
    string? Motivo,
    DateTime CreatedAt);

public record ResumoFormaPagamentoDto(FormaPagamento Forma, decimal Total);

public record ResumoFechamentoDto(
    string SessaoCaixaId,
    decimal ValorInicial,
    IReadOnlyList<ResumoFormaPagamentoDto> VendasPorForma,
    decimal TotalSuprimentos,
    decimal TotalSangrias,
    decimal ValorEsperadoDinheiro,
    decimal ValorContado,
    decimal Diferenca,
    DateTime FechadaEm);
