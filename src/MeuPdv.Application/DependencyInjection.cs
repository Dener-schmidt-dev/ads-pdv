using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Options;
using MeuPdv.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MeuPdv.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioAppService, UsuarioAppService>();
        services.AddSingleton<IProdutoImagemStorage, ProdutoImagemStorage>();
        services.AddSingleton<IPdvPersonalizacaoStorage, PdvPersonalizacaoStorage>();
        services.AddScoped<IProdutoAppService, ProdutoAppService>();
        services.AddScoped<ICategoriaAppService, CategoriaAppService>();
        services.AddScoped<IEstoqueAppService, EstoqueAppService>();
        services.AddScoped<ICaixaAppService, CaixaAppService>();
        services.AddScoped<IVendaAppService, VendaAppService>();
        services.AddScoped<IClienteAppService, ClienteAppService>();
        services.AddScoped<IConfiguracaoAppService, ConfiguracaoAppService>();
        services.AddScoped<IRelatorioAppService, RelatorioAppService>();
        services.AddScoped<IBackupAppService, BackupAppService>();
        services.AddScoped<ILicenseAppService, LicenseAppService>();
        return services;
    }
}
