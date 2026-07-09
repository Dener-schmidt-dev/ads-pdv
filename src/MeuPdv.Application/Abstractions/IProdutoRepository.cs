using MeuPdv.Domain.Entities;

namespace MeuPdv.Application.Abstractions;

public interface IProdutoRepository : IRepository<Produto>
{
    Task<Produto?> GetByCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default);
    Task<Produto?> GetByCodigoInternoAsync(string codigoInterno, CancellationToken ct = default);
    Task<IReadOnlyList<Produto>> BuscarPorDescricaoAsync(string termo, int limite = 10, CancellationToken ct = default);
    Task<(IReadOnlyList<Produto> Itens, long Total)> BuscarPaginadoAsync(
        string? termo,
        bool? ativo,
        bool? abaixoMinimo,
        int skip,
        int take,
        CancellationToken ct = default);
    Task<int> ObterProximoCodigoInternoAsync(CancellationToken ct = default);
}
