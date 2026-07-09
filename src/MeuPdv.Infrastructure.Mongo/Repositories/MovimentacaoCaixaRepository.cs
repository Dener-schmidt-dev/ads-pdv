using MeuPdv.Application.Abstractions;
using MeuPdv.Domain.Entities;
using MongoDB.Driver;

namespace MeuPdv.Infrastructure.Mongo.Repositories;

public class MovimentacaoCaixaRepository : MongoRepository<MovimentacaoCaixa>, IMovimentacaoCaixaRepository
{
    public MovimentacaoCaixaRepository(MongoDbContext context) : base(context, MongoCollections.MovimentacoesCaixa) { }

    public async Task<IReadOnlyList<MovimentacaoCaixa>> ListarPorSessaoAsync(
        string sessaoCaixaId, CancellationToken ct = default) =>
        await Collection.Find(x => x.SessaoCaixaId == sessaoCaixaId)
            .SortBy(x => x.CreatedAt)
            .ToListAsync(ct);
}
