using MeuPdv.Domain.Enums;

namespace MeuPdv.Application.Vendas;

public record ProdutoLeituraDto(
    string Id,
    string CodigoInterno,
    string? CodigoBarras,
    string Descricao,
    decimal PrecoVenda,
    decimal EstoqueAtual,
    string Unidade,
    bool TemImagem);

public record VendaItemRequest(
    string ProdutoId,
    decimal Quantidade);

public record PagamentoRequest(
    FormaPagamento Forma,
    decimal Valor,
    decimal ValorRecebido);

public record FinalizarVendaRequest(
    string CaixaIdentificador,
    string? ClienteId,
    IReadOnlyList<VendaItemRequest> Itens,
    decimal Desconto,
    string? MotivoDesconto,
    IReadOnlyList<PagamentoRequest> Pagamentos);

public record FinalizarVendaResponse(
    string VendaId,
    string Numero,
    decimal Total,
    decimal Troco,
    string? ComprovanteTexto,
    PixQrResultadoDto? Pix,
    bool ImprimirComprovante);

public record VendaListaDto(
    string Id,
    string Numero,
    DateTime Data,
    decimal Total,
    string Status,
    string FormasPagamento);

public record VendaListaResponse(
    IReadOnlyList<VendaListaDto> Itens,
    long Total);

public record VendaDetalheDto(
    string Id,
    string Numero,
    DateTime Data,
    decimal Subtotal,
    decimal Desconto,
    decimal Total,
    string Status,
    string? MotivoCancelamento,
    IReadOnlyList<VendaItemDetalheDto> Itens,
    IReadOnlyList<PagamentoDetalheDto> Pagamentos);

public record VendaItemDetalheDto(
    string Descricao,
    decimal Quantidade,
    decimal PrecoUnitario,
    decimal Total);

public record PagamentoDetalheDto(string Forma, decimal Valor);

public record CancelarVendaRequest(string Motivo);

public record CancelarVendaResponse(
    string VendaId,
    string Numero,
    decimal TotalEstornado);

public record PixQrResultadoDto(string CopiaCola, string? QrCodeBase64);

public record ClienteDto(string Id, string Nome, string? Cpf, string? Telefone);

public record CriarClienteRapidoRequest(string Nome, string? Cpf, string? Telefone);
