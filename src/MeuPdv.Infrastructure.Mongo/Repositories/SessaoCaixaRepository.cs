using MeuPdv.Application.Abstractions;
using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Enums;
using MongoDB.Driver;

namespace MeuPdv.Infrastructure.Mongo.Repositories;

public class SessaoCaixaRepository : MongoRepository<SessaoCaixa>, ISessaoCaixaRepository
{
    public SessaoCaixaRepository(MongoDbContext context) : base(context, MongoCollections.SessoesCaixa) { }

    public async Task<SessaoCaixa?> ObterAbertaAsync(string caixaIdentificador, CancellationToken ct = default) =>
        await Collection.Find(x =>
            x.CaixaIdentificador == caixaIdentificador &&
            x.Status == StatusSessaoCaixa.Aberta).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<SessaoCaixa>> ListarHistoricoAsync(int skip, int take, CancellationToken ct = default) =>
        await Collection.Find(FilterDefinition<SessaoCaixa>.Empty)
            .SortByDescending(x => x.AbertaEm)
            .Skip(skip)
            .Limit(take)
            .ToListAsync(ct);
}
