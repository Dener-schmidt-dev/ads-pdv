using MeuPdv.Domain.Entities;

namespace MeuPdv.Application.Abstractions;

public interface IConfiguracaoSistemaRepository
{
    Task<ConfiguracaoSistema> ObterAsync(CancellationToken ct = default);
    Task SalvarAsync(ConfiguracaoSistema config, CancellationToken ct = default);
}
