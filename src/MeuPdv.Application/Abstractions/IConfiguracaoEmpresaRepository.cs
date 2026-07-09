using MeuPdv.Domain.Entities;

namespace MeuPdv.Application.Abstractions;

public interface IConfiguracaoEmpresaRepository
{
    Task<ConfiguracaoEmpresa> ObterAsync(CancellationToken ct = default);
    Task SalvarAsync(ConfiguracaoEmpresa config, CancellationToken ct = default);
}
