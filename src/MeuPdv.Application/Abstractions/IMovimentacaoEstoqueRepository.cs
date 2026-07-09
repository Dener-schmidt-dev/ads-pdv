using MeuPdv.Domain.Entities;

namespace MeuPdv.Application.Abstractions;

public interface IMovimentacaoEstoqueRepository : IRepository<MovimentacaoEstoque>
{
    Task<bool> ExisteIdempotencyKeyAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<MovimentacaoEstoque>> ListarPorProdutoAsync(string produtoId, int skip, int take, CancellationToken ct = default);
    Task<IReadOnlyList<MovimentacaoEstoque>> ListarRecentesAsync(int skip, int take, CancellationToken ct = default);
}
