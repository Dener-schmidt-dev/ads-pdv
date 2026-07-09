using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Backup;
using MeuPdv.Application.Options;
using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MeuPdv.Application.Services;

public class BackupAppService : IBackupAppService
{
    private static readonly SemaphoreSlim Lock = new(1, 1);

    private readonly IHistoricoBackupRepository _historicoRepository;
    private readonly IConfiguracaoSistemaRepository _configuracaoRepository;
    private readonly IBackupExecutor _backupExecutor;
    private readonly MongoDbOptions _mongoOptions;
    private readonly MeuPdvPathsOptions _pathsOptions;
    private readonly ILogger<BackupAppService> _logger;

    public BackupAppService(
        IHistoricoBackupRepository historicoRepository,
        IConfiguracaoSistemaRepository configuracaoRepository,
        IBackupExecutor backupExecutor,
        IOptions<MongoDbOptions> mongoOptions,
        IOptions<MeuPdvPathsOptions> pathsOptions,
        ILogger<BackupAppService> logger)
    {
        _historicoRepository = historicoRepository;
        _configuracaoRepository = configuracaoRepository;
        _backupExecutor = backupExecutor;
        _mongoOptions = mongoOptions.Value;
        _pathsOptions = pathsOptions.Value;
        _logger = logger;
    }

    public async Task<BackupStatusDto> ObterStatusAsync(CancellationToken ct = default)
    {
        var config = await _configuracaoRepository.ObterAsync(ct);
        var diretorio = ResolverDiretorioBackup(config);
        var ultimo = await _historicoRepository.ObterUltimoSucessoAsync(ct);

        return new BackupStatusDto(
            diretorio,
            config.IntervaloBackupMinutos,
            config.DiasRetencaoBackup,
            ultimo?.CreatedAt,
            ultimo?.Sucesso ?? false,
            _backupExecutor.FerramentasDisponiveis() ? "mongodump/mongorestore" : null);
    }

    public async Task<IReadOnlyList<BackupHistoricoDto>> ListarHistoricoAsync(CancellationToken ct = default)
    {
        var itens = await _historicoRepository.ListarRecentesAsync(50, ct);
        return itens.Select(Mapear).ToList();
    }

    public Task<ExecutarBackupResultadoDto> ExecutarManualAsync(CancellationToken ct = default) =>
        ExecutarBackupInternoAsync(automatico: false, ct);

    public async Task<ExecutarBackupResultadoDto> RestaurarAsync(
        string historicoId,
        RestaurarBackupRequest request,
        CancellationToken ct = default)
    {
        if (!request.Confirmar)
            throw new DomainException("Confirme a restauração para substituir os dados atuais.");

        var historico = await _historicoRepository.GetByIdAsync(historicoId, ct)
            ?? throw new DomainException("Backup não encontrado.");

        if (!historico.Sucesso)
            throw new DomainException("Somente backups bem-sucedidos podem ser restaurados.");

        if (!File.Exists(historico.CaminhoArquivo))
            throw new DomainException("Arquivo de backup não encontrado no disco.");

        await Lock.WaitAsync(ct);
        try
        {
            _logger.LogWarning("Iniciando restauração do backup {BackupId} — {Arquivo}", historicoId, historico.CaminhoArquivo);

            var resultado = await _backupExecutor.ExecutarRestoreAsync(
                _mongoOptions.ConnectionString,
                historico.CaminhoArquivo,
                ct);

            return new ExecutarBackupResultadoDto(
                historico.Id,
                resultado.Sucesso,
                resultado.MensagemErro,
                Path.GetFileName(historico.CaminhoArquivo),
                resultado.TamanhoBytes);
        }
        finally
        {
            Lock.Release();
        }
    }

    public async Task VerificarBackupAutomaticoAsync(CancellationToken ct = default)
    {
        var config = await _configuracaoRepository.ObterAsync(ct);
        var ultimo = await _historicoRepository.ObterUltimoSucessoAsync(ct);
        var intervalo = TimeSpan.FromMinutes(Math.Max(60, config.IntervaloBackupMinutos));

        if (ultimo is not null && DateTime.UtcNow - ultimo.CreatedAt < intervalo)
            return;

        await ExecutarBackupInternoAsync(automatico: true, ct);
    }

    private async Task<ExecutarBackupResultadoDto> ExecutarBackupInternoAsync(bool automatico, CancellationToken ct)
    {
        if (!await Lock.WaitAsync(0, ct))
        {
            return new ExecutarBackupResultadoDto(
                string.Empty,
                false,
                "Já existe um backup ou restauração em andamento.",
                null,
                0);
        }

        HistoricoBackup historico = new()
        {
            Automatico = automatico,
            Sucesso = false
        };

        try
        {
            var config = await _configuracaoRepository.ObterAsync(ct);
            var diretorio = ResolverDiretorioBackup(config);
            Directory.CreateDirectory(diretorio);

            var nomeArquivo = $"meupdv-{DateTime.Now:yyyyMMdd-HHmmss}.archive.gz";
            var caminhoCompleto = Path.Combine(diretorio, nomeArquivo);
            historico.CaminhoArquivo = caminhoCompleto;

            var resultado = await _backupExecutor.ExecutarDumpAsync(
                _mongoOptions.ConnectionString,
                _mongoOptions.DatabaseName,
                caminhoCompleto,
                ct);

            historico.Sucesso = resultado.Sucesso;
            historico.MensagemErro = resultado.MensagemErro;
            historico.TamanhoBytes = resultado.TamanhoBytes;

            await _historicoRepository.InsertAsync(historico, ct);

            if (resultado.Sucesso)
            {
                _logger.LogInformation(
                    "Backup {Tipo} concluído: {Arquivo} ({Tamanho} bytes)",
                    automatico ? "automático" : "manual",
                    caminhoCompleto,
                    resultado.TamanhoBytes);

                await AplicarRetencaoAsync(diretorio, config.DiasRetencaoBackup, ct);
            }
            else
            {
                _logger.LogWarning("Falha no backup {Tipo}: {Erro}", automatico ? "automático" : "manual", resultado.MensagemErro);
            }

            return new ExecutarBackupResultadoDto(
                historico.Id,
                historico.Sucesso,
                historico.MensagemErro,
                nomeArquivo,
                historico.TamanhoBytes);
        }
        catch (Exception ex)
        {
            historico.MensagemErro = ex.Message;
            historico.Sucesso = false;

            if (string.IsNullOrEmpty(historico.CaminhoArquivo))
                historico.CaminhoArquivo = string.Empty;

            await _historicoRepository.InsertAsync(historico, ct);
            _logger.LogError(ex, "Erro ao executar backup");

            return new ExecutarBackupResultadoDto(
                historico.Id,
                false,
                ex.Message,
                null,
                0);
        }
        finally
        {
            Lock.Release();
        }
    }

    private async Task AplicarRetencaoAsync(string diretorio, int diasRetencao, CancellationToken ct)
    {
        var limite = DateTime.UtcNow.AddDays(-Math.Max(1, diasRetencao));
        var historicos = await _historicoRepository.ListarRecentesAsync(500, ct);

        foreach (var item in historicos.Where(h => h.Sucesso && h.CreatedAt < limite))
        {
            if (!string.IsNullOrWhiteSpace(item.CaminhoArquivo) && File.Exists(item.CaminhoArquivo))
            {
                try { File.Delete(item.CaminhoArquivo); }
                catch (Exception ex) { _logger.LogWarning(ex, "Não foi possível excluir {Arquivo}", item.CaminhoArquivo); }
            }

            await _historicoRepository.RemoverAsync(item.Id, ct);
        }
    }

    private string ResolverDiretorioBackup(ConfiguracaoSistema config)
    {
        if (!string.IsNullOrWhiteSpace(config.DiretorioBackup))
            return config.DiretorioBackup.Trim();

        if (!string.IsNullOrWhiteSpace(_pathsOptions.BackupsDirectory))
            return _pathsOptions.BackupsDirectory;

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "MeuPDV",
            "backups");
    }

    private static BackupHistoricoDto Mapear(HistoricoBackup h) => new(
        h.Id,
        string.IsNullOrWhiteSpace(h.CaminhoArquivo) ? "(sem arquivo)" : Path.GetFileName(h.CaminhoArquivo),
        h.CaminhoArquivo,
        h.TamanhoBytes,
        h.Sucesso,
        h.MensagemErro,
        h.Automatico,
        h.CreatedAt);
}
