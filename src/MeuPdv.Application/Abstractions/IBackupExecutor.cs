namespace MeuPdv.Application.Abstractions;

public interface IBackupExecutor
{
    bool FerramentasDisponiveis();

    Task<BackupExecucaoResult> ExecutarDumpAsync(
        string connectionString,
        string databaseName,
        string arquivoDestino,
        CancellationToken ct = default);

    Task<BackupExecucaoResult> ExecutarRestoreAsync(
        string connectionString,
        string arquivoOrigem,
        CancellationToken ct = default);
}

public record BackupExecucaoResult(bool Sucesso, string? MensagemErro, long TamanhoBytes);
