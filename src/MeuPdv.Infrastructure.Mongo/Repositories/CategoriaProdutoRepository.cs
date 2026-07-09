using MeuPdv.Application.Abstractions;
using MeuPdv.Domain.Entities;
using MongoDB.Driver;

namespace MeuPdv.Infrastructure.Mongo.Repositories;

public class CategoriaProdutoRepository : MongoRepository<CategoriaProduto>, ICategoriaProdutoRepository
{
    public CategoriaProdutoRepository(MongoDbContext context) : base(context, MongoCollections.Categorias) { }

    public async Task<CategoriaProduto?> GetByNomeAsync(string nome, CancellationToken ct = default) =>
        await Collection.Find(x => x.Nome == nome).FirstOrDefaultAsync(ct);
}
