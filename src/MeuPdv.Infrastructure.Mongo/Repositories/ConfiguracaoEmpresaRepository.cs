using MeuPdv.Application.Abstractions;
using MeuPdv.Domain.Entities;
using MongoDB.Driver;

namespace MeuPdv.Infrastructure.Mongo.Repositories;

public class ConfiguracaoEmpresaRepository : IConfiguracaoEmpresaRepository
{
    private readonly IMongoCollection<ConfiguracaoEmpresa> _collection;

    public ConfiguracaoEmpresaRepository(MongoDbContext context) =>
        _collection = context.GetCollection<ConfiguracaoEmpresa>(MongoCollections.ConfiguracaoEmpresa);

    public async Task<ConfiguracaoEmpresa> ObterAsync(CancellationToken ct = default)
    {
        var config = await _collection.Find(FilterDefinition<ConfiguracaoEmpresa>.Empty).FirstOrDefaultAsync(ct);
        if (config is not null)
            return config;

        config = new ConfiguracaoEmpresa { NomeEstabelecimento = "MeuPDV" };
        await _collection.InsertOneAsync(config, cancellationToken: ct);
        return config;
    }

    public async Task SalvarAsync(ConfiguracaoEmpresa config, CancellationToken ct = default)
    {
        config.UpdatedAt = DateTime.UtcNow;
        await _collection.ReplaceOneAsync(x => x.Id == config.Id, config, cancellationToken: ct);
    }
}
