using MeuPdv.Application.Abstractions;
using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MeuPdv.Infrastructure.Mongo.Repositories;

public class VendaRepository : MongoRepository<Venda>, IVendaRepository
{
    public VendaRepository(MongoDbContext context) : base(context, MongoCollections.Vendas) { }

    public async Task<long> ObterProximoNumeroAsync(CancellationToken ct = default)
    {
        var ultima = await Collection.Find(FilterDefinition<Venda>.Empty)
            .SortByDescending(x => x.Numero)
            .Limit(1)
            .FirstOrDefaultAsync(ct);

        if (ultima is null || !long.TryParse(ultima.Numero, out var numero))
            return 1;

        return numero + 1;
    }

    public async Task<(IReadOnlyList<Venda> Itens, long Total)> ListarConcluidasPaginadoAsync(
        string? termo, int skip, int take, CancellationToken ct = default)
    {
        var filter = Builders<Venda>.Filter.Eq(x => x.Status, StatusVenda.Concluida);
        if (!string.IsNullOrWhiteSpace(termo))
            filter &= Builders<Venda>.Filter.Regex(x => x.Numero, new BsonRegularExpression(termo.Trim(), "i"));

        var total = await Collection.CountDocumentsAsync(filter, cancellationToken: ct);
        var itens = await Collection.Find(filter)
            .SortByDescending(x => x.ConcluidaEm)
            .ThenByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Limit(take)
            .ToListAsync(ct);

        return (itens, total);
    }
}
