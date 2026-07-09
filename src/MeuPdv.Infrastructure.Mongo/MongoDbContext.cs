using MeuPdv.Application.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace MeuPdv.Infrastructure.Mongo;

public class MongoDbContext
{
    public IMongoDatabase Database { get; }

    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        var settings = options.Value;
        var client = new MongoClient(settings.ConnectionString);
        Database = client.GetDatabase(settings.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string name) => Database.GetCollection<T>(name);
}
