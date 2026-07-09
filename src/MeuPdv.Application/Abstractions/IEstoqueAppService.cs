using MeuPdv.Application.Common;
using MeuPdv.Application.Estoque;

namespace MeuPdv.Application.Abstractions;

public interface IEstoqueAppService
{
    Task<SaldoProdutoDto?> ConsultarSaldoAsync(string produtoId, CancellationToken ct = default);
    Task<IReadOnlyList<SaldoProdutoDto>> ListarSaldosAsync(string? termo, bool? abaixoMinimo, CancellationToken ct = default);
    Task<IReadOnlyList<AlertaEstoqueDto>> ListarAlertasMinimoAsync(CancellationToken ct = default);
    Task<MovimentacaoEstoqueDto> RegistrarEntradaAsync(MovimentacaoManualRequest request, string operadorId, CancellationToken ct = default);
    Task<MovimentacaoEstoqueDto> RegistrarSaidaAsync(MovimentacaoManualRequest request, string operadorId, CancellationToken ct = default);
    Task<MovimentacaoEstoqueDto> RegistrarAjusteAsync(AjusteEstoqueRequest request, string operadorId, CancellationToken ct = default);
    Task<PaginaListaDto<MovimentacaoEstoqueDto>> HistoricoAsync(string? produtoId, int pagina, int tamanho, CancellationToken ct = default);
}
