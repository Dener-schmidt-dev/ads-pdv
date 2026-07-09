using MeuPdv.Application.Abstractions;
using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Helpers;
using MongoDB.Driver;

namespace MeuPdv.Infrastructure.Mongo.Repositories;

public class ClienteRepository : MongoRepository<Cliente>, IClienteRepository
{
    public ClienteRepository(MongoDbContext context) : base(context, MongoCollections.Clientes) { }

    public async Task<IReadOnlyList<Cliente>> BuscarAsync(string termo, int limite = 10, CancellationToken ct = default)
    {
        var t = termo.Trim();
        var normalizado = TextNormalizer.Normalizar(t);

        var filters = new List<FilterDefinition<Cliente>>
        {
            Builders<Cliente>.Filter.Eq(x => x.Ativo, true)
        };

        var termFilters = new List<FilterDefinition<Cliente>>();
        if (!string.IsNullOrWhiteSpace(normalizado))
        {
            termFilters.Add(Builders<Cliente>.Filter.Regex(
                x => x.Nome, new MongoDB.Bson.BsonRegularExpression(normalizado, "i")));
        }

        termFilters.Add(Builders<Cliente>.Filter.Eq(x => x.Cpf, t));
        termFilters.Add(Builders<Cliente>.Filter.Eq(x => x.Telefone, t));

        filters.Add(Builders<Cliente>.Filter.Or(termFilters));

        return await Collection.Find(Builders<Cliente>.Filter.And(filters))
            .Limit(limite)
            .ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<Cliente> Itens, long Total)> ListarPaginadoAsync(
        string? termo,
        bool? ativo,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var filters = new List<FilterDefinition<Cliente>>();

        if (ativo.HasValue)
            filters.Add(Builders<Cliente>.Filter.Eq(x => x.Ativo, ativo.Value));

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim();
            var normalizado = TextNormalizer.Normalizar(t);
            var termFilters = new List<FilterDefinition<Cliente>>
            {
                Builders<Cliente>.Filter.Eq(x => x.Cpf, t),
                Builders<Cliente>.Filter.Eq(x => x.Telefone, t)
            };

            if (!string.IsNullOrWhiteSpace(normalizado))
            {
                termFilters.Add(Builders<Cliente>.Filter.Regex(
                    x => x.Nome, new MongoDB.Bson.BsonRegularExpression(normalizado, "i")));
            }

            filters.Add(Builders<Cliente>.Filter.Or(termFilters));
        }

        var filter = filters.Count > 0
            ? Builders<Cliente>.Filter.And(filters)
            : FilterDefinition<Cliente>.Empty;

        var total = await Collection.CountDocumentsAsync(filter, cancellationToken: ct);
        var itens = await Collection.Find(filter)
            .SortBy(x => x.Nome)
            .Skip(skip)
            .Limit(take)
            .ToListAsync(ct);

        return (itens, total);
    }

    public async Task<Cliente?> GetByCpfAsync(string cpf, CancellationToken ct = default) =>
        await Collection.Find(x => x.Cpf == cpf).FirstOrDefaultAsync(ct);
}
