using MeuPdv.Application.Abstractions;
using MeuPdv.Domain.Entities;
using MongoDB.Driver;

namespace MeuPdv.Infrastructure.Mongo.Repositories;

public class UsuarioRepository : MongoRepository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(MongoDbContext context) : base(context, MongoCollections.Usuarios) { }

    public async Task<Usuario?> GetByLoginAsync(string login, CancellationToken ct = default) =>
        await Collection.Find(x => x.Login == login).FirstOrDefaultAsync(ct);
}
