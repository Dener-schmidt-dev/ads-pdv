using MeuPdv.Application.Backup;

namespace MeuPdv.Application.Abstractions;

public interface IBackupAppService
{
    Task<BackupStatusDto> ObterStatusAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BackupHistoricoDto>> ListarHistoricoAsync(CancellationToken ct = default);
    Task<ExecutarBackupResultadoDto> ExecutarManualAsync(CancellationToken ct = default);
    Task<ExecutarBackupResultadoDto> RestaurarAsync(string historicoId, RestaurarBackupRequest request, CancellationToken ct = default);
    Task VerificarBackupAutomaticoAsync(CancellationToken ct = default);
}
