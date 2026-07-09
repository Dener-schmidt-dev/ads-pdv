using System.Linq.Expressions;
using MeuPdv.Application.Abstractions;
using MeuPdv.Domain.Common;
using MongoDB.Driver;

namespace MeuPdv.Infrastructure.Mongo.Repositories;

public abstract class MongoRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly IMongoCollection<T> Collection;

    protected MongoRepository(MongoDbContext context, string collectionName)
    {
        Collection = context.GetCollection<T>(collectionName);
    }

    public virtual async Task<T?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await Collection.Find(x => x.Id == id).FirstOrDefaultAsync(ct);

    public virtual async Task<IReadOnlyList<T>> ListAsync(
        Expression<Func<T, bool>>? filter = null,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default)
    {
        var query = filter is null
            ? Collection.Find(FilterDefinition<T>.Empty)
            : Collection.Find(filter);

        return await query.Skip(skip).Limit(take).ToListAsync(ct);
    }

    public virtual async Task<long> CountAsync(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default)
    {
        var query = filter is null
            ? Collection.Find(FilterDefinition<T>.Empty)
            : Collection.Find(filter);

        return await query.CountDocumentsAsync(ct);
    }

    public virtual async Task InsertAsync(T entity, CancellationToken ct = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await Collection.InsertOneAsync(entity, cancellationToken: ct);
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        await Collection.ReplaceOneAsync(x => x.Id == entity.Id, entity, cancellationToken: ct);
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default) =>
        await Collection.Find(filter).AnyAsync(ct);
}
