namespace MeuPdv.Application.Backup;

public record BackupHistoricoDto(
    string Id,
    string NomeArquivo,
    string CaminhoArquivo,
    long TamanhoBytes,
    bool Sucesso,
    string? MensagemErro,
    bool Automatico,
    DateTime CreatedAt);

public record ExecutarBackupResultadoDto(
    string Id,
    bool Sucesso,
    string? Mensagem,
    string? NomeArquivo,
    long TamanhoBytes);

public record BackupStatusDto(
    string DiretorioBackup,
    int IntervaloBackupMinutos,
    int DiasRetencaoBackup,
    DateTime? UltimoBackupEm,
    bool UltimoBackupSucesso,
    string? FerramentaDisponivel);

public record RestaurarBackupRequest(bool Confirmar);
