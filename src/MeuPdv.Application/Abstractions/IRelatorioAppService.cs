using MeuPdv.Application.Relatorios;

namespace MeuPdv.Application.Abstractions;

public interface IRelatorioAppService
{
    Task<VendasPorPeriodoDto> VendasPorPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task<byte[]> ExportarVendasCsvAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);

    Task<ProdutosMaisVendidosDto> ProdutosMaisVendidosAsync(
        DateTime inicio, DateTime fim, int limite = 20, CancellationToken ct = default);

    Task<byte[]> ExportarProdutosMaisVendidosCsvAsync(
        DateTime inicio, DateTime fim, int limite = 100, CancellationToken ct = default);

    Task<IReadOnlyList<EstoqueBaixoItemDto>> EstoqueBaixoAsync(CancellationToken ct = default);
    Task<byte[]> ExportarEstoqueBaixoCsvAsync(CancellationToken ct = default);
}
