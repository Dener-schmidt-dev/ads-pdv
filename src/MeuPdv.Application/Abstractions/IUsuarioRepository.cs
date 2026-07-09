using MeuPdv.Domain.Entities;

namespace MeuPdv.Application.Abstractions;

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> GetByLoginAsync(string login, CancellationToken ct = default);
}
