using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Options;
using MeuPdv.Infrastructure.Mongo.HostedServices;
using MeuPdv.Infrastructure.Mongo.Indexing;
using MeuPdv.Infrastructure.Mongo.Repositories;
using MeuPdv.Infrastructure.Mongo.Seed;
using MeuPdv.Infrastructure.Mongo.Security;
using MeuPdv.Infrastructure.Mongo.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MeuPdv.Infrastructure.Mongo;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureMongo(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoDbOptions>(configuration.GetSection(MongoDbOptions.SectionName));
        services.Configure<MeuPdvPathsOptions>(configuration.GetSection(MeuPdvPathsOptions.SectionName));

        services.AddSingleton<MongoDbContext>();
        services.AddSingleton<MongoIndexInitializer>();
        services.AddScoped<DevelopmentSeedService>();

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IProdutoRepository, ProdutoRepository>();
        services.AddScoped<ICategoriaProdutoRepository, CategoriaProdutoRepository>();
        services.AddScoped<IMovimentacaoEstoqueRepository, MovimentacaoEstoqueRepository>();
        services.AddScoped<IConfiguracaoSistemaRepository, ConfiguracaoSistemaRepository>();
        services.AddScoped<ISessaoCaixaRepository, SessaoCaixaRepository>();
        services.AddScoped<IMovimentacaoCaixaRepository, MovimentacaoCaixaRepository>();
        services.AddScoped<IVendaRepository, VendaRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IConfiguracaoEmpresaRepository, ConfiguracaoEmpresaRepository>();
        services.AddScoped<IHistoricoBackupRepository, HistoricoBackupRepository>();
        services.AddScoped<IBackupExecutor, MongoBackupExecutor>();
        services.AddHostedService<BackupAutomaticoHostedService>();
        services.AddScoped<IPixQrService, PixQrService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ILicenseService, LicenseService>();
        services.AddSingleton<IFiscalService, FiscalServiceDisabled>();

        return services;
    }
}
