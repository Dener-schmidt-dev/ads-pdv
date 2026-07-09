using MeuPdv.Domain.Entities;

namespace MeuPdv.Application.Abstractions;

public interface IHistoricoBackupRepository : IRepository<HistoricoBackup>
{
    Task<IReadOnlyList<HistoricoBackup>> ListarRecentesAsync(int limite = 50, CancellationToken ct = default);
    Task<HistoricoBackup?> ObterUltimoSucessoAsync(CancellationToken ct = default);
    Task RemoverAsync(string id, CancellationToken ct = default);
}
