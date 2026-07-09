using MeuPdv.Application.Abstractions;
using MeuPdv.Domain.Entities;
using MongoDB.Driver;

namespace MeuPdv.Infrastructure.Mongo.Repositories;

public class MovimentacaoEstoqueRepository : MongoRepository<MovimentacaoEstoque>, IMovimentacaoEstoqueRepository
{
    public MovimentacaoEstoqueRepository(MongoDbContext context) : base(context, MongoCollections.MovimentacoesEstoque) { }

    public async Task<bool> ExisteIdempotencyKeyAsync(string key, CancellationToken ct = default) =>
        await Collection.Find(x => x.IdempotencyKey == key).AnyAsync(ct);

    public async Task<IReadOnlyList<MovimentacaoEstoque>> ListarPorProdutoAsync(
        string produtoId, int skip, int take, CancellationToken ct = default) =>
        await Collection.Find(x => x.ProdutoId == produtoId)
            .SortByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Limit(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<MovimentacaoEstoque>> ListarRecentesAsync(
        int skip, int take, CancellationToken ct = default) =>
        await Collection.Find(FilterDefinition<MovimentacaoEstoque>.Empty)
            .SortByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Limit(take)
            .ToListAsync(ct);
}
