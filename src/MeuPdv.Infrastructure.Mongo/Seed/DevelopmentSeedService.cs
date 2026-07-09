using MeuPdv.Application.Abstractions;
using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Enums;
using MeuPdv.Domain.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MeuPdv.Infrastructure.Mongo.Seed;

public class DevelopmentSeedService
{
    private readonly IHostEnvironment _environment;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly MongoDbContext _context;
    private readonly ILogger<DevelopmentSeedService> _logger;

    public DevelopmentSeedService(
        IHostEnvironment environment,
        IUsuarioRepository usuarioRepository,
        IProdutoRepository produtoRepository,
        MongoDbContext context,
        ILogger<DevelopmentSeedService> logger)
    {
        _environment = environment;
        _usuarioRepository = usuarioRepository;
        _produtoRepository = produtoRepository;
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedUsuarioAdminAsync(ct);
        await SeedConfiguracoesAsync(ct);

        if (!_environment.IsDevelopment())
            return;

        await SeedProdutosAsync(ct);
        await SeedLicencaAsync(ct);
    }

    private async Task SeedUsuarioAdminAsync(CancellationToken ct)
    {
        if (await _usuarioRepository.ExistsAsync(x => x.Login == "admin", ct))
            return;

        var admin = new Usuario
        {
            Login = "admin",
            Nome = "Administrador",
            SenhaHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Perfil = PerfilUsuario.Administrador,
            Ativo = true
        };

        await _usuarioRepository.InsertAsync(admin, ct);
        _logger.LogInformation("Usuário administrador inicial criado (login: admin).");
    }

    private async Task SeedProdutosAsync(CancellationToken ct)
    {
        var produtos = new (string Barras, string Descricao, decimal Preco)[]
        {
            ("7894900011517", "Coca-Cola 2L", 8.99m),
            ("7891234567890", "Arroz Branco 5kg", 23.90m),
            ("7891234567807", "Feijão Carioca 1kg", 7.49m),
            ("7891025110025", "Leite Integral 1L", 4.49m),
            ("7896005309873", "Óleo de Soja 900ml", 6.49m)
        };

        var codigo = 1;
        foreach (var (barras, descricao, preco) in produtos)
        {
            if (await _produtoRepository.ExistsAsync(x => x.CodigoBarras == barras, ct))
                continue;

            await _produtoRepository.InsertAsync(new Produto
            {
                CodigoInterno = codigo.ToString("D4"),
                CodigoBarras = barras,
                Descricao = descricao,
                DescricaoNormalizada = TextNormalizer.Normalizar(descricao),
                Unidade = "UN",
                PrecoVenda = preco,
                EstoqueAtual = 100,
                EstoqueMinimo = 10,
                Ativo = true
            }, ct);

            codigo++;
        }

        _logger.LogInformation("Produtos de desenvolvimento verificados.");
    }

    private async Task SeedConfiguracoesAsync(CancellationToken ct)
    {
        var empresaCol = _context.GetCollection<ConfiguracaoEmpresa>(MongoCollections.ConfiguracaoEmpresa);
        if (!await empresaCol.Find(FilterDefinition<ConfiguracaoEmpresa>.Empty).AnyAsync(ct))
        {
            await empresaCol.InsertOneAsync(new ConfiguracaoEmpresa
            {
                NomeEstabelecimento = "MeuPDV Demo",
                ChavePix = "demo@meupdv.local",
                TipoChavePix = Domain.Enums.TipoChavePix.Email
            }, cancellationToken: ct);
        }

        var sistemaCol = _context.GetCollection<ConfiguracaoSistema>(MongoCollections.ConfiguracaoSistema);
        if (!await sistemaCol.Find(FilterDefinition<ConfiguracaoSistema>.Empty).AnyAsync(ct))
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            await sistemaCol.InsertOneAsync(new ConfiguracaoSistema
            {
                DiretorioBackup = Path.Combine(programData, "MeuPDV", "backups"),
                IntervaloBackupMinutos = 1440,
                DiasRetencaoBackup = 30,
                LimiteDescontoOperadorPercentual = 5m,
                TemaVisual = "claro"
            }, cancellationToken: ct);
        }
    }

    private async Task SeedLicencaAsync(CancellationToken ct)
    {
        var col = _context.GetCollection<LicencaLocal>(MongoCollections.LicencaLocal);
        if (await col.Find(FilterDefinition<LicencaLocal>.Empty).AnyAsync(ct))
            return;

        await col.InsertOneAsync(new LicencaLocal
        {
            Chave = "DEV-MODE",
            Validade = DateTime.UtcNow.AddYears(10),
            ModoDesenvolvimento = true,
            UltimaValidacao = DateTime.UtcNow
        }, cancellationToken: ct);
    }
}
