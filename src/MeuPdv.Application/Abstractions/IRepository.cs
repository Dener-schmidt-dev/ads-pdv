using System.Linq.Expressions;
using MeuPdv.Domain.Common;

namespace MeuPdv.Application.Abstractions;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? filter = null, int skip = 0, int take = 50, CancellationToken ct = default);
    Task<long> CountAsync(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default);
    Task InsertAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> filter, CancellationToken ct = default);
}
