using MeuPdv.Application.Vendas;

namespace MeuPdv.Application.Abstractions;

public interface IVendaAppService
{
    Task<ProdutoLeituraDto?> LerProdutoAsync(string codigo, CancellationToken ct = default);
    Task<IReadOnlyList<ProdutoLeituraDto>> BuscarProdutosAsync(string termo, int limite = 10, CancellationToken ct = default);
    Task<FinalizarVendaResponse> FinalizarAsync(
        FinalizarVendaRequest request,
        string operadorId,
        bool podeConcederDesconto,
        CancellationToken ct = default);
    Task<VendaListaResponse> ListarRecentesAsync(
        string? termo, int pagina = 1, int tamanho = 20, CancellationToken ct = default);
    Task<VendaDetalheDto?> ObterAsync(string id, CancellationToken ct = default);
    Task<CancelarVendaResponse> CancelarAsync(string id, CancelarVendaRequest request, string operadorId, CancellationToken ct = default);
}
