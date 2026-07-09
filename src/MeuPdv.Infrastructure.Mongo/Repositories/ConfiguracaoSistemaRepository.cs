using MeuPdv.Application.Abstractions;
using MeuPdv.Domain.Entities;
using MongoDB.Driver;

namespace MeuPdv.Infrastructure.Mongo.Repositories;

public class ConfiguracaoSistemaRepository : IConfiguracaoSistemaRepository
{
    private readonly IMongoCollection<ConfiguracaoSistema> _collection;

    public ConfiguracaoSistemaRepository(MongoDbContext context) =>
        _collection = context.GetCollection<ConfiguracaoSistema>(MongoCollections.ConfiguracaoSistema);

    public async Task<ConfiguracaoSistema> ObterAsync(CancellationToken ct = default)
    {
        var config = await _collection.Find(FilterDefinition<ConfiguracaoSistema>.Empty).FirstOrDefaultAsync(ct);
        if (config is not null)
            return config;

        config = new ConfiguracaoSistema
        {
            DiretorioBackup = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "MeuPDV", "backups")
        };

        await _collection.InsertOneAsync(config, cancellationToken: ct);
        return config;
    }

    public async Task SalvarAsync(ConfiguracaoSistema config, CancellationToken ct = default)
    {
        config.UpdatedAt = DateTime.UtcNow;
        await _collection.ReplaceOneAsync(x => x.Id == config.Id, config, cancellationToken: ct);
    }
}
