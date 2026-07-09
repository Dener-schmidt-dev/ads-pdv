using MeuPdv.Application.Abstractions;
using MeuPdv.Domain.Entities;
using MongoDB.Driver;

namespace MeuPdv.Infrastructure.Mongo.Repositories;

public class HistoricoBackupRepository : MongoRepository<HistoricoBackup>, IHistoricoBackupRepository
{
    public HistoricoBackupRepository(MongoDbContext context)
        : base(context, MongoCollections.HistoricoBackup) { }

    public async Task<IReadOnlyList<HistoricoBackup>> ListarRecentesAsync(int limite = 50, CancellationToken ct = default) =>
        await Collection.Find(FilterDefinition<HistoricoBackup>.Empty)
            .SortByDescending(x => x.CreatedAt)
            .Limit(Math.Clamp(limite, 1, 200))
            .ToListAsync(ct);

    public async Task<HistoricoBackup?> ObterUltimoSucessoAsync(CancellationToken ct = default) =>
        await Collection.Find(x => x.Sucesso)
            .SortByDescending(x => x.CreatedAt)
            .Limit(1)
            .FirstOrDefaultAsync(ct);

    public async Task RemoverAsync(string id, CancellationToken ct = default) =>
        await Collection.DeleteOneAsync(x => x.Id == id, ct);
}
