using MeuPdv.Application.Abstractions;
using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Helpers;
using MongoDB.Driver;

namespace MeuPdv.Infrastructure.Mongo.Repositories;

public class ProdutoRepository : MongoRepository<Produto>, IProdutoRepository
{
    public ProdutoRepository(MongoDbContext context) : base(context, MongoCollections.Produtos) { }

    public async Task<Produto?> GetByCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default) =>
        await Collection.Find(x => x.CodigoBarras == codigoBarras && x.Ativo).FirstOrDefaultAsync(ct);

    public async Task<Produto?> GetByCodigoInternoAsync(string codigoInterno, CancellationToken ct = default) =>
        await Collection.Find(x => x.CodigoInterno == codigoInterno && x.Ativo).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Produto>> BuscarPorDescricaoAsync(string termo, int limite = 10, CancellationToken ct = default)
    {
        var normalizado = TextNormalizer.Normalizar(termo);
        if (string.IsNullOrWhiteSpace(normalizado))
            return [];

        var filter = Builders<Produto>.Filter.And(
            Builders<Produto>.Filter.Eq(x => x.Ativo, true),
            Builders<Produto>.Filter.Regex(x => x.DescricaoNormalizada, new MongoDB.Bson.BsonRegularExpression(normalizado, "i")));

        return await Collection.Find(filter).Limit(limite).ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<Produto> Itens, long Total)> BuscarPaginadoAsync(
        string? termo,
        bool? ativo,
        bool? abaixoMinimo,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var filters = new List<FilterDefinition<Produto>>();

        if (ativo.HasValue)
            filters.Add(Builders<Produto>.Filter.Eq(x => x.Ativo, ativo.Value));

        if (abaixoMinimo == true)
            filters.Add(Builders<Produto>.Filter.Where(x => x.EstoqueAtual < x.EstoqueMinimo));

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim();
            var normalizado = TextNormalizer.Normalizar(t);
            var termFilters = new List<FilterDefinition<Produto>>
            {
                Builders<Produto>.Filter.Eq(x => x.CodigoBarras, t),
                Builders<Produto>.Filter.Eq(x => x.CodigoInterno, t)
            };

            if (!string.IsNullOrWhiteSpace(normalizado))
            {
                termFilters.Add(Builders<Produto>.Filter.Regex(
                    x => x.DescricaoNormalizada,
                    new MongoDB.Bson.BsonRegularExpression(normalizado, "i")));
            }

            filters.Add(Builders<Produto>.Filter.Or(termFilters));
        }

        var filter = filters.Count > 0
            ? Builders<Produto>.Filter.And(filters)
            : FilterDefinition<Produto>.Empty;

        var total = await Collection.CountDocumentsAsync(filter, cancellationToken: ct);
        var itens = await Collection.Find(filter)
            .SortBy(x => x.Descricao)
            .Skip(skip)
            .Limit(take)
            .ToListAsync(ct);

        return (itens, total);
    }

    public async Task<int> ObterProximoCodigoInternoAsync(CancellationToken ct = default)
    {
        var ultimo = await Collection.Find(FilterDefinition<Produto>.Empty)
            .SortByDescending(x => x.CodigoInterno)
            .Limit(1)
            .FirstOrDefaultAsync(ct);

        if (ultimo is null || !int.TryParse(ultimo.CodigoInterno, out var codigo))
            return 1;

        return codigo + 1;
    }
}
