using MeuPdv.Application.Produtos;

namespace MeuPdv.Application.Abstractions;

public interface IProdutoAppService
{
    Task<ProdutoListaResponse> ListarAsync(string? termo, bool? ativo, bool? abaixoMinimo, int pagina, int tamanho, CancellationToken ct = default);
    Task<ProdutoDto?> ObterPorIdAsync(string id, CancellationToken ct = default);
    Task<ProdutoDto> CriarAsync(CriarProdutoRequest request, string operadorId, CancellationToken ct = default);
    Task<ProdutoDto> CriarRapidoAsync(CriarProdutoRapidoRequest request, string operadorId, CancellationToken ct = default);
    Task<ProdutoDto> AtualizarAsync(string id, AtualizarProdutoRequest request, string operadorId, bool podeAlterarPreco, CancellationToken ct = default);
    Task InativarAsync(string id, string operadorId, CancellationToken ct = default);
    Task<ImportacaoCsvResultado> ImportarCsvAsync(Stream csv, string operadorId, CancellationToken ct = default);
    Task<byte[]> ExportarCsvAsync(CancellationToken ct = default);
    Task<ProdutoDto> SalvarImagemAsync(string id, Stream arquivo, long tamanhoBytes, string contentType, string? nomeArquivo, string operadorId, CancellationToken ct = default);
    Task<ProdutoDto> RemoverImagemAsync(string id, string operadorId, CancellationToken ct = default);
    Task<(string Caminho, string ContentType)?> ObterImagemArquivoAsync(string id, CancellationToken ct = default);
}
