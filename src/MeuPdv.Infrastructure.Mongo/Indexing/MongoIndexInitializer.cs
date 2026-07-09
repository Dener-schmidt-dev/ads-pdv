using MeuPdv.Domain.Entities;
using MongoDB.Driver;

namespace MeuPdv.Infrastructure.Mongo.Indexing;

public class MongoIndexInitializer
{
    private readonly MongoDbContext _context;

    public MongoIndexInitializer(MongoDbContext context) => _context = context;

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await CreateUsuarioIndexes(ct);
        await CreateProdutoIndexes(ct);
        await CreateVendaIndexes(ct);
        await CreateCaixaIndexes(ct);
        await CreateMovimentacaoCaixaIndexes(ct);
        await CreateClienteIndexes(ct);
        await CreateCategoriaIndexes(ct);
        await CreateEstoqueIndexes(ct);
        await CreateBackupIndexes(ct);
    }

    private async Task CreateUsuarioIndexes(CancellationToken ct)
    {
        var col = _context.GetCollection<Usuario>(MongoCollections.Usuarios);
        await col.Indexes.CreateOneAsync(
            new CreateIndexModel<Usuario>(Builders<Usuario>.IndexKeys.Ascending(x => x.Login),
                new CreateIndexOptions { Unique = true }), cancellationToken: ct);
    }

    private async Task CreateProdutoIndexes(CancellationToken ct)
    {
        var col = _context.GetCollection<Produto>(MongoCollections.Produtos);
        var keys = Builders<Produto>.IndexKeys;
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Produto>(keys.Ascending(x => x.CodigoBarras)),
            new CreateIndexModel<Produto>(keys.Ascending(x => x.CodigoInterno), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<Produto>(keys.Ascending(x => x.DescricaoNormalizada))
        ], cancellationToken: ct);
    }

    private async Task CreateVendaIndexes(CancellationToken ct)
    {
        var col = _context.GetCollection<Venda>(MongoCollections.Vendas);
        var keys = Builders<Venda>.IndexKeys;
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Venda>(keys.Descending(x => x.CreatedAt)),
            new CreateIndexModel<Venda>(keys.Ascending(x => x.SessaoCaixaId)),
            new CreateIndexModel<Venda>(keys.Ascending(x => x.OperadorId)),
            new CreateIndexModel<Venda>(
                keys.Ascending(x => x.Status).Descending(x => x.ConcluidaEm))
        ], cancellationToken: ct);
    }

    private async Task CreateCaixaIndexes(CancellationToken ct)
    {
        var col = _context.GetCollection<SessaoCaixa>(MongoCollections.SessoesCaixa);
        await col.Indexes.CreateOneAsync(
            new CreateIndexModel<SessaoCaixa>(
                Builders<SessaoCaixa>.IndexKeys.Ascending(x => x.CaixaIdentificador).Ascending(x => x.Status)),
            cancellationToken: ct);
    }

    private async Task CreateMovimentacaoCaixaIndexes(CancellationToken ct)
    {
        var col = _context.GetCollection<MovimentacaoCaixa>(MongoCollections.MovimentacoesCaixa);
        var keys = Builders<MovimentacaoCaixa>.IndexKeys;
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<MovimentacaoCaixa>(keys.Ascending(x => x.SessaoCaixaId).Descending(x => x.CreatedAt)),
            new CreateIndexModel<MovimentacaoCaixa>(keys.Descending(x => x.CreatedAt))
        ], cancellationToken: ct);
    }

    private async Task CreateClienteIndexes(CancellationToken ct)
    {
        var col = _context.GetCollection<Cliente>(MongoCollections.Clientes);
        var keys = Builders<Cliente>.IndexKeys;
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Cliente>(keys.Ascending(x => x.Cpf),
                new CreateIndexOptions { Unique = true, Sparse = true }),
            new CreateIndexModel<Cliente>(keys.Ascending(x => x.Telefone),
                new CreateIndexOptions { Sparse = true }),
            new CreateIndexModel<Cliente>(keys.Ascending(x => x.Ativo).Ascending(x => x.Nome))
        ], cancellationToken: ct);
    }

    private async Task CreateCategoriaIndexes(CancellationToken ct)
    {
        var col = _context.GetCollection<CategoriaProduto>(MongoCollections.Categorias);
        await col.Indexes.CreateOneAsync(
            new CreateIndexModel<CategoriaProduto>(
                Builders<CategoriaProduto>.IndexKeys.Ascending(x => x.Nome),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: ct);
    }

    private async Task CreateBackupIndexes(CancellationToken ct)
    {
        var col = _context.GetCollection<HistoricoBackup>(MongoCollections.HistoricoBackup);
        await col.Indexes.CreateOneAsync(
            new CreateIndexModel<HistoricoBackup>(
                Builders<HistoricoBackup>.IndexKeys.Descending(x => x.CreatedAt)),
            cancellationToken: ct);
    }

    private async Task CreateEstoqueIndexes(CancellationToken ct)
    {
        var col = _context.GetCollection<MovimentacaoEstoque>(MongoCollections.MovimentacoesEstoque);
        var keys = Builders<MovimentacaoEstoque>.IndexKeys;
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<MovimentacaoEstoque>(keys.Descending(x => x.CreatedAt)),
            new CreateIndexModel<MovimentacaoEstoque>(keys.Ascending(x => x.ProdutoId)),
            new CreateIndexModel<MovimentacaoEstoque>(keys.Ascending(x => x.IdempotencyKey),
                new CreateIndexOptions { Unique = true })
        ], cancellationToken: ct);
    }
}
